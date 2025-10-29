using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.Commands.CreateProduct;
using R2.ShopNet.Catalog.Infrastructure.Persistence;
using R2.ShopNet.Framework.Configuration;
using R2.ShopNet.Framework.Configuration.Integration;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.DependencyInjection;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.UnitOfWork;
using R2.ShopNet.Framework.ServiceDiscovery;
using Serilog;

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

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "R2.ShopNet Catalog Service API",
            Version = "v1",
            Description = "Product catalog and inventory management service"
        });
    });

    // Configure Database
    var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
        ?? "Host=localhost;Port=5432;Database=r2shopnet_catalog;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<CatalogDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Register UnitOfWork
    builder.Services.AddScoped<IUnitOfWork>(sp =>
        new UnitOfWork(sp.GetRequiredService<CatalogDbContext>()));

    // Register CQRS Handlers automatically using reflection
    builder.Services.AddCQRSHandlersFromAssemblyContaining<CreateProductCommandHandler>();

    // Add Event Publisher
    builder.Services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

    // Configure Consul Service Discovery
    builder.Services.AddConsulServiceDiscovery(builder.Configuration);

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();

    // Service discovery registration will happen automatically via the AddConsulServiceDiscovery extension

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
