using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using R2.ShopNet.Gateway.API.Configuration;
using R2.ShopNet.Gateway.API.HealthChecks;
using R2.ShopNet.Gateway.API.Middleware;
using R2.ShopNet.Gateway.API.Services;
using Serilog;
using Yarp.ReverseProxy.Configuration;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting R2.ShopNet API Gateway");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Bind configuration options
    builder.Services.Configure<ConsulOptions>(
        builder.Configuration.GetSection(ConsulOptions.SectionName));
    builder.Services.Configure<GatewayOptions>(
        builder.Configuration.GetSection(GatewayOptions.SectionName));

    // Add Aspire service discovery
    builder.Services.AddServiceDiscovery();
    
    // Configure HTTP client to use service discovery
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });

    // Add HTTP client for Consul
    builder.Services.AddHttpClient("consul", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // Add YARP reverse proxy
    // In Development, use static config from appsettings.Development.json
    // In Production, use Consul service discovery
    if (builder.Environment.IsProduction())
    {
        // Add Consul service discovery provider
        builder.Services.AddSingleton<IProxyConfigProvider, ConsulServiceDiscoveryProvider>();
        builder.Services.AddReverseProxy();
    }
    else
    {
        // Use static configuration for development (with Aspire service discovery)
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
    }

    // Add Consul registration service
    builder.Services.AddHostedService<ConsulRegistrationService>();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<ConsulHealthCheck>("consul");

    // Add CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:4200" };
    
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Add JWT Authentication (optional - uncomment when Identity Service is ready)
    /*
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var authConfig = builder.Configuration.GetSection("Authentication");
            options.Authority = authConfig["Authority"];
            options.Audience = authConfig["Audience"];
            options.RequireHttpsMetadata = authConfig.GetValue<bool>("RequireHttpsMetadata", true);
            
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });

    builder.Services.AddAuthorization();
    */

    // Add Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
        var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
        var window = rateLimitConfig.GetValue<TimeSpan>("Window", TimeSpan.FromMinutes(1));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var partitionKey = context.User.Identity?.Name 
                ?? context.Connection.RemoteIpAddress?.ToString() 
                ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "RATE_LIMIT_EXCEEDED",
                    message = "Too many requests. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                        ? (double?)retryAfter.TotalSeconds 
                        : null
                }
            }, cancellationToken: token);
        };
    });

    // Add OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("R2.ShopNet.Gateway"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter();
                // Uncomment for Jaeger export
                // .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
        });

    var app = builder.Build();

    // Middleware pipeline (ORDER MATTERS!)
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseCors();

    // Uncomment when authentication is configured
    // app.UseAuthentication();
    // app.UseAuthorization();

    app.UseRateLimiter();

    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/ready");

    // Map reverse proxy
    app.MapReverseProxy();

    // Welcome endpoint
    app.MapGet("/", () => new
    {
        service = "R2.ShopNet API Gateway",
        version = "1.0.0",
        status = "running",
        timestamp = DateTime.UtcNow
    });

    Log.Information("API Gateway configured and ready to start");
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
