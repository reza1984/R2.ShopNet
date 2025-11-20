using R2.ShopNet.Identity.Application.Interfaces;
using R2.ShopNet.Identity.Infrastructure.Services;
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
using Serilog;
using R2.ShopNet.Framework.Logging;

// Configure Serilog with R2.ShopNet defaults
Log.Logger = LoggingConfiguration.CreateBootstrapLogger("Identity.API");

try
{
    Log.Information("Starting Identity Service");

    var builder = WebApplication.CreateBuilder(args);

    // Add key-value store (Consul) as configuration source
    // This integrates Consul KV directly into IConfiguration
    builder.Configuration.AddKeyValueConfiguration("identity/");

    // Add Serilog with R2.ShopNet configuration
    builder.AddSerilog("Identity.API");

    builder.AddServiceDefaults();

    // Add services to the container
    builder.Services.AddHealthChecks();
    builder.Services.AddControllers();

    // Add MVC and Razor Pages with runtime compilation for development hot reload
    var mvcBuilder = builder.Services.AddControllersWithViews();
    var razorPagesBuilder = builder.Services.AddRazorPages();

    // Enable Razor Runtime Compilation for hot reload in development
    if (builder.Environment.IsDevelopment())
    {
        mvcBuilder.AddRazorRuntimeCompilation();
        razorPagesBuilder.AddRazorRuntimeCompilation();
    }

    builder.Services.AddEndpointsApiExplorer();

    // Add Redis distributed cache for passkey challenge storage
    // Aspire injects the connection string via .WithReference(redis) as ConnectionStrings:redis
    var redisConnection = builder.Configuration.GetConnectionString("redis")
        ?? throw new InvalidOperationException("Redis connection string not found. Ensure Aspire AppHost has .WithReference(redis) configured.");

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "R2ShopNet:Identity:";
    });
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "R2.ShopNet Identity Service API",
            Version = "v1",
            Description = "Authentication and user management service"
        });
    });

    // Register HttpContextAccessor
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
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

    // Configure Identity cookies to support cross-origin requests
    builder.Services.ConfigureApplicationCookie(options =>
    {
        // Use None for cross-origin support (required for Gateway/Angular scenarios)
        options.Cookie.SameSite = SameSiteMode.None;
        // Always require HTTPS in both dev and production for security
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true; // Prevent JavaScript access
        options.Cookie.IsEssential = true; // Required for GDPR
    });

    // Configure Authentication to use OpenIddict validation as the default scheme
    // Also add Cookie authentication for authorization flow login page
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "R2.ShopNet.Identity";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Ensure proper redirect on challenge (not 401)
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                Console.WriteLine($"Cookie Auth: Redirecting to login: {context.RedirectUri}");
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
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
            // Enable endpoints for both password flow (Angular) and authorization code flow (Blazor)
            options.SetAuthorizationEndpointUris("/connect/authorize")
                   .SetTokenEndpointUris("/connect/token")
                   .SetUserinfoEndpointUris("/connect/userinfo")
                   .SetLogoutEndpointUris("/connect/endsession");

            // Enable flows: Authorization Code (Blazor), Password (Angular), Refresh Token, Custom Passkey
            options.AllowAuthorizationCodeFlow()
                   .AllowPasswordFlow()
                   .AllowRefreshTokenFlow()
                   .AllowCustomFlow("urn:ietf:params:oauth:grant-type:passkey");

            // Require PKCE for public clients (enhanced security for authorization code flow)
            options.RequireProofKeyForCodeExchange();

            // Accept anonymous clients for backward compatibility (Angular uses anonymous)
            // Blazor portal will use confidential client with secret
            options.AcceptAnonymousClients();

            // Register scopes
            options.RegisterScopes(
                OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Scopes.Roles,
                "api",
                "admin");

            // Register the signing and encryption credentials
            // Note: Encryption certificate is still required by OpenIddict even when encryption is disabled
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
            
            // Disable access token encryption for development (allows standard JWT Bearer validation)
            options.DisableAccessTokenEncryption();

            // Register the ASP.NET Core host and configure options
            options.UseAspNetCore()
                   .EnableAuthorizationEndpointPassthrough()
                   .EnableTokenEndpointPassthrough()
                   .EnableUserinfoEndpointPassthrough()
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

    builder.Services.AddScoped<IPasskeyService, PasskeyService>();
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
            policy.WithOrigins(
                "http://localhost:4200",   // Angular admin (dev)
                "https://localhost:4200",  // Angular admin (SSL)
                "https://localhost:5000",  // Gateway
                "https://localhost:5007"   // Blazor portal (NEW)
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        });
    });

    // Configure Consul Service Discovery
    builder.Services.AddConsulServiceDiscovery(builder.Configuration);

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

    // Add detailed request logging middleware for debugging
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        // Also log to console for immediate visibility
        Console.WriteLine($"=== Incoming Request ===");
        Console.WriteLine($"Method: {context.Request.Method}");
        Console.WriteLine($"Path: {context.Request.Path}");
        Console.WriteLine($"QueryString: {context.Request.QueryString}");
        Console.WriteLine($"Scheme: {context.Request.Scheme}");
        Console.WriteLine($"Host: {context.Request.Host}");

        logger.LogInformation("=== Incoming Request ===");
        logger.LogInformation("Method: {Method}", context.Request.Method);
        logger.LogInformation("Path: {Path}", context.Request.Path);
        logger.LogInformation("QueryString: {QueryString}", context.Request.QueryString);
        logger.LogInformation("Scheme: {Scheme}", context.Request.Scheme);
        logger.LogInformation("Host: {Host}", context.Request.Host);

        // Log cookies
        if (context.Request.Cookies.Any())
        {
            var cookieInfo = string.Join(", ", context.Request.Cookies.Select(c => $"{c.Key}=[Present]"));
            Console.WriteLine($"Cookies: {cookieInfo}");
            logger.LogInformation("Cookies: {Cookies}", cookieInfo);
        }
        else
        {
            Console.WriteLine("No cookies present");
            logger.LogInformation("No cookies present");
        }

        await next();

        Console.WriteLine($"Response Status: {context.Response.StatusCode}");
        logger.LogInformation("Response Status: {StatusCode}", context.Response.StatusCode);

        // Log redirect location if present
        if (context.Response.Headers.ContainsKey("Location"))
        {
            Console.WriteLine($"Redirect Location: {context.Response.Headers["Location"]}");
            logger.LogInformation("Redirect Location: {Location}", context.Response.Headers["Location"]);
        }

        Console.WriteLine(); // Empty line for readability
    });

    // Redirect HTTP to HTTPS
    app.UseHttpsRedirection();

    // Enable static files (required for wwwroot folder - CSS, JS, images)
    app.UseStaticFiles();

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapRazorPages();  // Enable Razor Pages for login UI

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
