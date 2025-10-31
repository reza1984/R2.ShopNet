using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.API.Extensions;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Configuration.Integration;
using R2.ShopNet.Framework.Events;
using Serilog;
using Microsoft.IdentityModel.Tokens;

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


    builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5000"; // IdentityServer URL
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
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
