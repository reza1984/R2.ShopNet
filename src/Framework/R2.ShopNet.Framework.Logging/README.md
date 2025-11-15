# R2.ShopNet.Framework.Logging

A comprehensive Serilog-based logging framework for R2.ShopNet microservices applications.

## Features

- **Serilog Integration**: Structured logging with Serilog
- **Multiple Sinks**: Console, File, and Seq support
- **Rich Enrichers**: Machine name, environment, thread ID, correlation ID
- **Easy Configuration**: Simple setup via appsettings.json or code
- **Extension Methods**: Convenient logging context management

## Installation

Add a reference to this project in your application:

```bash
dotnet add reference ../R2.ShopNet.Framework.Logging/R2.ShopNet.Framework.Logging.csproj
```

## Quick Start

### 1. Configure in Program.cs

```csharp
using R2.ShopNet.Framework.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog with R2.ShopNet defaults
builder.AddSerilog("YourServiceName");

// Or create a bootstrap logger for early startup logging
Log.Logger = LoggingConfiguration.CreateBootstrapLogger("YourServiceName");

try
{
    Log.Information("Starting application");

    var app = builder.Build();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### 2. Configure in appsettings.json

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Application}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "apiKey": "your-api-key-here"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentName"]
  },
  "Logging": {
    "R2ShopNet": {
      "ApplicationName": "YourServiceName",
      "EnableFileLogging": true,
      "LogFilePath": "logs/app-.log",
      "EnableSeqLogging": false,
      "SeqServerUrl": "http://localhost:5341",
      "MinimumLevel": "Information"
    }
  }
}
```

## Usage Examples

### Basic Logging

```csharp
public class MyService
{
    private readonly ILogger _logger = Log.ForContext<MyService>();

    public void DoWork()
    {
        _logger.Information("Starting work");

        try
        {
            // Do work
            _logger.Debug("Work in progress");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Work failed");
        }

        _logger.Information("Work completed");
    }
}
```

### Structured Logging

```csharp
_logger.Information("User {UserId} created order {OrderId}", userId, orderId);
```

### Using Correlation ID

```csharp
using (logger.WithCorrelationId(correlationId))
{
    _logger.Information("Processing request");
    // All logs within this scope will include the correlation ID
}
```

### Using Custom Properties

```csharp
var properties = new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["TenantId"] = tenantId,
    ["OrderId"] = orderId
};

using (logger.WithProperties(properties))
{
    _logger.Information("Processing order");
}
```

### In ASP.NET Core Controllers

```csharp
public class ProductsController : ControllerBase
{
    private readonly ILogger _logger = Log.ForContext<ProductsController>();

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        using (logger.WithCorrelationId(HttpContext.TraceIdentifier))
        {
            _logger.Information("Getting product {ProductId}", id);

            // Your logic here

            return Ok(product);
        }
    }
}
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| ApplicationName | Name shown in logs | "R2.ShopNet" |
| EnableFileLogging | Enable file sink | true |
| LogFilePath | Path to log files | "logs/app-.log" |
| EnableSeqLogging | Enable Seq sink | false |
| SeqServerUrl | Seq server URL | null |
| SeqApiKey | Seq API key | null |
| MinimumLevel | Minimum log level | "Information" |

## Enrichers

The framework automatically enriches logs with:

- **Application Name**: Identifies which service generated the log
- **Machine Name**: Hostname of the machine
- **Environment Name**: Development, Staging, Production
- **Thread ID**: Thread identifier
- **Correlation ID**: Request tracking (when using extensions)

## Best Practices

1. **Use structured logging**: Always use property placeholders instead of string interpolation
   ```csharp
   // Good
   _logger.Information("User {UserId} logged in", userId);

   // Bad
   _logger.Information($"User {userId} logged in");
   ```

2. **Use correlation IDs**: Track requests across service boundaries

3. **Log at appropriate levels**:
   - **Debug**: Detailed information for debugging
   - **Information**: General informational messages
   - **Warning**: Warning messages for recoverable issues
   - **Error**: Error messages for exceptions
   - **Fatal**: Fatal errors that require immediate attention

4. **Always close and flush**: Ensure logs are written before application shutdown
   ```csharp
   Log.CloseAndFlush();
   ```

## License

Part of the R2.ShopNet microservices framework.
