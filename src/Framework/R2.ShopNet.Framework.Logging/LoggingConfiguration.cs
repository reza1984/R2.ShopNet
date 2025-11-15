using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace R2.ShopNet.Framework.Logging;

/// <summary>
/// Provides Serilog configuration and initialization for R2.ShopNet applications
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog with sensible defaults for R2.ShopNet applications
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    /// <param name="applicationName">The name of the application (used for logging context)</param>
    /// <returns>A configured LoggerConfiguration</returns>
    public static LoggerConfiguration ConfigureSerilog(
        IConfiguration configuration,
        string applicationName)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId();

        // Add default console sink if not configured
        if (!HasSinkConfigured(configuration))
        {
            loggerConfiguration.WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        return loggerConfiguration;
    }

    /// <summary>
    /// Adds Serilog to the host builder with R2.ShopNet defaults
    /// </summary>
    /// <param name="hostBuilder">The host builder</param>
    /// <param name="applicationName">The name of the application</param>
    /// <returns>The host builder for chaining</returns>
    public static IHostBuilder AddSerilog(
        this IHostBuilder hostBuilder,
        string applicationName)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            ConfigureSerilog(context.Configuration, applicationName)
                .ReadFrom.Services(services)
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();
        });
    }

    /// <summary>
    /// Adds Serilog to the application builder with R2.ShopNet defaults
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <param name="applicationName">The name of the application</param>
    public static void AddSerilog(
        this Microsoft.AspNetCore.Builder.WebApplicationBuilder builder,
        string applicationName)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            var logConfig = ConfigureSerilog(context.Configuration, applicationName);

            logConfig.ReadFrom.Services(services);

            // Configure output template with application name
            if (!HasSinkConfigured(context.Configuration))
            {
                logConfig.WriteTo.Console(
                    theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            }
        });
    }

    /// <summary>
    /// Creates a bootstrap logger for early application startup logging
    /// </summary>
    /// <param name="applicationName">The name of the application</param>
    /// <returns>A configured logger</returns>
    public static ILogger CreateBootstrapLogger(string applicationName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Checks if any sinks are configured in the configuration
    /// </summary>
    private static bool HasSinkConfigured(IConfiguration configuration)
    {
        var serilogSection = configuration.GetSection("Serilog");
        var writeTo = serilogSection.GetSection("WriteTo");
        return writeTo.Exists() && writeTo.GetChildren().Any();
    }
}
