using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.API.Extensions;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Configuration.Integration;
using R2.ShopNet.Framework.Events;
using Serilog;
using OpenIddict.Validation.AspNetCore;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/catalog-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Catalog Service");

    var builder = WebApplication.CreateBuilder(args);

    // Add key-value store (Consul) as configuration source
    builder.Configuration.AddKeyValueConfiguration("catalog/");

    // Add Serilog
    builder.Host.UseSerilog();

    builder.AddServiceDefaults();

    // Grouped service registrations
    builder.Services
        .AddCatalogHealthChecks()
        .AddCatalogControllers()
        .AddCatalogPersistence(builder.Configuration)
        .AddCatalogObjectStorage(builder.Configuration)
        .AddCatalogDomain()
        .AddCatalogEventing()
        .AddCatalogServiceDiscovery(builder.Configuration)
        .AddCatalogCors();


    // Configure OpenIddict validation (handles both JWS and JWE tokens)
    builder.Services.AddOpenIddict()
        .AddValidation(options =>
        {
            // Note: The validation handler uses OpenID Connect discovery to retrieve
            // the signing keys from Identity service's /.well-known/openid-configuration
            options.SetIssuer("https://localhost:5003/");
            
            // Don't validate audience in development (tokens may not have audience claim)
            options.Configure(validationOptions =>
            {
                validationOptions.TokenValidationParameters.ValidateAudience = false;
            });
            
            options.UseSystemNetHttp();
            
            // Register the ASP.NET Core host
            options.UseAspNetCore();
        });
    
    // Add HTTP client handler to bypass SSL in development
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        });
    }

    // Configure authentication to use OpenIddict
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    });
    
    builder.Services.AddAuthorization();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.RegisterCatalogApiEndpoints();
    app.MapControllers();

    // Run database migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Database.Migrate();
        Log.Information("Database migrations applied");
    }

    Log.Information("Catalog Service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Catalog Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// InMemoryEventPublisher for now - move to Infrastructure later
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        _logger.LogInformation("Event published: {EventType} - {Event}",
            typeof(TEvent).Name, @event);
        return Task.CompletedTask;
    }

    public Task PublishMany(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            _logger.LogInformation("Event published: {EventType} - {Event}",
                @event.GetType().Name, @event);
        }
        return Task.CompletedTask;
    }
}

// Make Program accessible to integration tests
public partial class Program { }
