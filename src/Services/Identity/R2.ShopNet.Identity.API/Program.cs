using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.Configuration;
using R2.ShopNet.Framework.Configuration.Integration;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.DependencyInjection;
using R2.ShopNet.Framework.CQRS.Generated;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Identity.Application.Commands.LoginUser;
using R2.ShopNet.Framework.ServiceDiscovery;
using R2.ShopNet.Identity.API.Services;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Domain.Entities;
using R2.ShopNet.Identity.Infrastructure.Configuration;
using R2.ShopNet.Identity.Infrastructure.Events;
using R2.ShopNet.Identity.Infrastructure.Persistence;
using R2.ShopNet.Identity.Infrastructure.Seed;
using R2.ShopNet.Identity.Infrastructure.Services;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/identity-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Identity Service");

    var builder = WebApplication.CreateBuilder(args);

    // Add key-value store (Consul) as configuration source
    // This integrates Consul KV directly into IConfiguration
    builder.Configuration.AddKeyValueConfiguration("identity/");

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddHealthChecks();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "R2.ShopNet Identity Service API",
            Version = "v1",
            Description = "Authentication and user management service"
        });
    });

    // Register HttpContextAccessor (required for passkey validation)
    builder.Services.AddHttpContextAccessor();

    // Configure Database
    var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
        ?? "Host=localhost;Port=5432;Database=r2shopnet_identity;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<IdentityDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Configure ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = false; // Set to true in production
        options.SignIn.RequireConfirmedPhoneNumber = false;

        // REQUIRED: Enable passkey support by using Identity schema version 3
        // This includes the IdentityUserPasskey table in the model
        options.Stores.SchemaVersion = Microsoft.AspNetCore.Identity.IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

    // Configure Passkey options (built into ASP.NET Core 10 Identity)
    builder.Services.Configure<Microsoft.AspNetCore.Identity.IdentityPasskeyOptions>(options =>
    {
        // Set server domain to "localhost" to allow passkeys from any port on localhost
        // This enables Angular dev server (4200), Gateway (5000), and Identity service (5002) to work together
        options.ServerDomain = "localhost"; 
        options.AuthenticatorTimeout = TimeSpan.FromMinutes(5);
        options.ChallengeSize = 64; // bytes
    });

    // Configure Identity cookies to support cross-origin requests (required for passkey session)
    builder.Services.ConfigureApplicationCookie(options =>
    {
        // Use None for cross-origin support (required for Gateway/Angular scenarios)
        options.Cookie.SameSite = SameSiteMode.None;
        // Always require HTTPS in both dev and production for security
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true; // Prevent JavaScript access
        options.Cookie.IsEssential = true; // Required for GDPR
    });

    // Configure the Two-Factor cookie (used for passkey session state)
    // This is CRITICAL for passkey authentication to work across requests
    builder.Services.Configure<CookieAuthenticationOptions>(Microsoft.AspNetCore.Identity.IdentityConstants.TwoFactorUserIdScheme, options =>
    {
        // Use None for cross-origin support (required for Gateway/Angular scenarios)
        options.Cookie.SameSite = SameSiteMode.None;
        // Always require HTTPS in both dev and production for security
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Configure Authentication to use OpenIddict validation as the default scheme
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    });

    // Register Consul KV Store for Configuration
    builder.Services.AddConsulKeyValueStore(builder.Configuration);
    builder.Services.AddConfigurationManager();

    // Register key-value configuration providers in DI
    builder.Services.AddKeyValueConfigurationServices(builder.Configuration as IConfigurationRoot 
        ?? throw new InvalidOperationException("Configuration must be IConfigurationRoot"));

    // Register Identity configuration initializer (seeds and initializes on startup)
    builder.Services.AddKeyValueConfigurationInitializer<IdentityConfigurationInitializer>();

    // Configure OpenIddict
    builder.Services.AddOpenIddict()
        // Register the OpenIddict core components
        .AddCore(options =>
        {
            // Configure OpenIddict to use the Entity Framework Core stores and models
            options.UseEntityFrameworkCore()
                   .UseDbContext<IdentityDbContext>()
                   .ReplaceDefaultEntities<Guid>();
        })
        // Register the OpenIddict server components
        .AddServer(options =>
        {
            // Enable the token endpoint (required for Resource Owner Password flow)
            options.SetTokenEndpointUris("/connect/token");
            // Enable the OIDC logout endpoint
            options.SetLogoutEndpointUris("/connect/endsession");

            // Enable the Resource Owner Password Credentials flow (for login in Angular)
            options.AllowPasswordFlow()
                   .AllowRefreshTokenFlow();

            // Accept anonymous clients (clients without a client_secret)
            options.AcceptAnonymousClients();

            // Register the signing and encryption credentials
            // Note: Encryption certificate is still required by OpenIddict even when encryption is disabled
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
            
            // Disable access token encryption for development (allows standard JWT Bearer validation)
            options.DisableAccessTokenEncryption();

            // Register the ASP.NET Core host and configure options
            options.UseAspNetCore()
                   .EnableTokenEndpointPassthrough()
                   .EnableLogoutEndpointPassthrough()
                   .DisableTransportSecurityRequirement(); // Only for development!

            // Configure token lifetimes
            options.SetAccessTokenLifetime(TimeSpan.FromHours(1))
                   .SetRefreshTokenLifetime(TimeSpan.FromDays(14));
        })
        // Register the OpenIddict validation components
        .AddValidation(options =>
        {
            // Import the configuration from the local OpenIddict server instance
            options.UseLocalServer();

            // Register the ASP.NET Core host
            options.UseAspNetCore();
        });

    // Register Services - JWT configuration will be loaded from Consul
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-256-bit-secret-key-here-change-in-production!!";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "R2.ShopNet.Identity";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "R2.ShopNet";

    builder.Services.AddScoped<ITokenService>(sp =>
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        return new TokenService(userManager, jwtSecret, jwtIssuer, jwtAudience, expirationMinutes: 60);
    });

    // Register CQRS Handlers automatically using reflection (similar to MediatR)
    // This scans the Application assembly at startup and registers all command/query handlers
    // For long-running services like this, reflection-based registration is recommended
    builder.Services.AddCQRSHandlersFromAssemblyContaining<LoginUserCommandHandler>();  

    // Register Event Publisher (placeholder for now)
    builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

    // Configure Email Service
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
    builder.Services.AddScoped<IEmailService, EmailService>();

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200", "https://localhost:5000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for passkey session cookies
        });
    });

    // Configure Consul Service Discovery
    builder.Services.AddConsulServiceDiscovery(builder.Configuration);

    // Add session support (required for passkey authentication flow)
    // Passkey authentication requires session state to be maintained between
    // the /login/begin and /login/complete requests
    builder.Services.AddDistributedMemoryCache(); // Use in-memory cache for sessions (consider Redis in production)
    builder.Services.AddSession(options =>
    {
        options.Cookie.Name = ".R2ShopNet.Session";
        options.IdleTimeout = TimeSpan.FromMinutes(10);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        // Use None for cross-origin support (required for Gateway/Angular scenarios)
        options.Cookie.SameSite = SameSiteMode.None;
        // Always require HTTPS in both dev and production for security
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    var app = builder.Build();

    // Apply database migrations and seed data automatically on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            Log.Information("Applying database migrations...");
            dbContext.Database.Migrate();
            Log.Information("Database migrations applied successfully");

            Log.Information("Seeding database with initial data...");
            var seeder = new DatabaseSeeder(userManager, roleManager, logger);
            await seeder.SeedAsync();
            Log.Information("Database seeding completed successfully");

            Log.Information("Seeding OpenIddict clients and scopes...");
            var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
            var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();
            var openIddictLogger = services.GetRequiredService<ILogger<OpenIddictSeeder>>();
            var openIddictSeeder = new OpenIddictSeeder(applicationManager, scopeManager, openIddictLogger);
            await openIddictSeeder.SeedAsync();
            Log.Information("OpenIddict seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during database initialization");
            throw;
        }
    }

    // Configuration initialization is handled by ConsulConfigurationInitializer hosted service
    // It will seed Consul KV store on first run and initialize all Consul providers

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    // Redirect HTTP to HTTPS
    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    // Enable session middleware (MUST be before UseAuthentication)
    // This is required for passkey authentication to maintain state
    // between /login/begin and /login/complete requests
    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();

    Log.Information("Identity Service started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
