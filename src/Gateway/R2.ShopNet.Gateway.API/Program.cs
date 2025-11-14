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

    // Add YARP reverse proxy with Consul service discovery
    // Register Consul service discovery provider BEFORE AddReverseProxy
    builder.Services.AddSingleton<ConsulServiceDiscoveryProvider>();
    builder.Services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<ConsulServiceDiscoveryProvider>());

    // Add YARP - it will use our IProxyConfigProvider
    builder.Services.AddReverseProxy();

    // Add Consul registration service
    builder.Services.AddHostedService<ConsulRegistrationService>();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<ConsulHealthCheck>("consul");

    // Add CORS
    builder.Services.AddCors(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all localhost origins (for Aspire dynamic ports)
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    var uri = new Uri(origin);
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            });
        }
        else
        {
            // In production, use configured allowed origins
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();

            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        }
    });

    // Add JWT Authentication with OpenIddict validation
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
                ValidateAudience = false, // OpenIddict doesn't always include audience
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };

            // For development: accept self-signed certificates
            if (builder.Environment.IsDevelopment())
            {
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            }
        });

    builder.Services.AddAuthorization();

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

    // Enable authentication and authorization
    app.UseAuthentication();
    app.UseAuthorization();

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
