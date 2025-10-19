# Aspire Integration - Admin Portal

## Summary

The Angular 20 Admin Portal has been successfully integrated into the .NET Aspire AppHost for unified orchestration.

## Changes Made

### 1. AppHost.csproj Updates

Added the Aspire Node.js hosting package:

```xml
<PackageReference Include="Aspire.Hosting.NodeJs" Version="9.5.1" />
```

### 2. AppHost.cs Updates

Added the Admin Portal as a Node.js application:

```csharp
// Admin Portal (Angular 20)
var adminPortal = builder.AddNodeApp("admin-portal", "../Web/R2.ShopNet.Web.Admin", "start")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithExternalHttpEndpoints();
```

## How It Works

### Aspire Dashboard Integration

When you run the AppHost, Aspire will:

1. **Launch the Admin Portal** - Automatically run `npm start` in the Angular project
2. **Expose on Port 4200** - The app will be accessible at `http://localhost:4200`
3. **Monitor Health** - Track the application status in Aspire Dashboard
4. **Capture Logs** - Display console output from the Angular dev server
5. **Provide Metrics** - Show resource usage and performance data

### Service Discovery

The Admin Portal is now visible in the Aspire Dashboard alongside:
- Identity Service
- PostgreSQL
- Redis
- RabbitMQ
- Consul
- Elasticsearch
- Other infrastructure services

## Running the Application

### Option 1: Run Everything via Aspire

```bash
cd src/R2.ShopNet.AppHost
dotnet run
```

This will start:
- All infrastructure containers (Consul, PostgreSQL, Redis, RabbitMQ, etc.)
- Identity Service API
- **Admin Portal (Angular app on port 4200)**

Then open the Aspire Dashboard (usually `http://localhost:15XXX`) to see all services.

### Option 2: Run Admin Portal Standalone

```bash
cd src/Web/R2.ShopNet.Web.Admin
npm start
```

## Aspire Dashboard Benefits

### 1. Unified View
- See all services in one place
- Monitor health status
- View resource usage

### 2. Logs Aggregation
- Combined logs from all services
- Filter by service
- Real-time streaming

### 3. Environment Management
- Automatic service dependencies
- Environment variable injection
- Port management

### 4. Development Experience
- One command to start everything
- Hot reload for Angular changes
- Easy service discovery

## Admin Portal Configuration

### NPM Script (package.json)
```json
{
  "scripts": {
    "start": "ng serve",
    "build": "ng build"
  }
}
```

The `start` script is used by Aspire to launch the development server.

### Port Configuration
- **Default Port**: 4200
- **Configurable**: Via `PORT` environment variable
- **External Access**: Enabled with `WithExternalHttpEndpoints()`

## Service Architecture

```
┌─────────────────────────────────────────────┐
│         .NET Aspire AppHost                 │
├─────────────────────────────────────────────┤
│                                             │
│  ┌────────────────┐  ┌──────────────────┐  │
│  │ Identity API   │  │  Admin Portal    │  │
│  │ (Port 5002)    │  │  (Port 4200)     │  │
│  │ ASP.NET Core   │  │  Angular 20      │  │
│  └────────────────┘  └──────────────────┘  │
│           ↓                    ↓            │
│  ┌────────────────────────────────────┐    │
│  │      Infrastructure Services       │    │
│  │  - PostgreSQL (Databases)          │    │
│  │  - Redis (Caching)                 │    │
│  │  - RabbitMQ (Messaging)            │    │
│  │  - Consul (Service Discovery)      │    │
│  │  - Elasticsearch (Search)          │    │
│  └────────────────────────────────────┘    │
│                                             │
└─────────────────────────────────────────────┘
```

## Accessing Services

When running via Aspire:

| Service | URL | Description |
|---------|-----|-------------|
| **Admin Portal** | http://localhost:4200 | Angular admin dashboard |
| Identity API | https://localhost:5002 | User management API |
| Aspire Dashboard | http://localhost:15XXX | Aspire orchestration UI |
| Consul UI | http://localhost:8500 | Service discovery |
| pgAdmin | http://localhost:5050 | PostgreSQL admin |
| Redis Commander | http://localhost:8081 | Redis management |
| RabbitMQ Management | http://localhost:15672 | Message queue admin |

## Development Workflow

### 1. Start All Services
```bash
cd src/R2.ShopNet.AppHost
dotnet run
```

### 2. Access Admin Portal
Open `http://localhost:4200` in your browser

### 3. Monitor in Aspire Dashboard
- Check logs
- View metrics
- Monitor health

### 4. Hot Reload
Angular changes are automatically detected and reloaded without restarting Aspire.

## Benefits for Team Development

1. **Simplified Onboarding**
   - One command starts entire platform
   - No manual service orchestration
   - Consistent development environment

2. **Integrated Debugging**
   - All logs in one place
   - Easy correlation between services
   - Health monitoring

3. **Production-Like Environment**
   - All services running together
   - Service discovery configured
   - Infrastructure dependencies managed

4. **CI/CD Ready**
   - Aspire orchestration files version controlled
   - Easy to replicate in pipelines
   - Containerization support

## Troubleshooting

### Admin Portal Not Starting

1. **Check NPM Installation**
   ```bash
   cd src/Web/R2.ShopNet.Web.Admin
   npm install
   ```

2. **Verify Start Script**
   ```bash
   npm start
   ```

3. **Check Aspire Logs**
   - Open Aspire Dashboard
   - Navigate to "admin-portal"
   - View console output

### Port Conflicts

If port 4200 is in use:
```bash
# Stop other Angular apps
killall node

# Or change port in AppHost.cs
.WithHttpEndpoint(port: 4201, env: "PORT")
```

## Next Steps

1. **Add More Web Apps**
   - Shopping Web App
   - Warehouse Management App
   - Delivery App

2. **Configure CORS**
   - Allow admin portal origin in Identity API
   - Configure for production domains

3. **Add Health Checks**
   - Custom health endpoints for Angular app
   - Liveness and readiness probes

4. **Production Deployment**
   - Build optimized Angular bundle
   - Configure reverse proxy
   - Set up SSL certificates

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Aspire Node.js Hosting](https://learn.microsoft.com/dotnet/aspire/get-started/build-aspire-apps-with-nodejs)
- [Angular CLI Serve](https://angular.dev/cli/serve)

---

**Status**: ✅ Successfully Integrated  
**Date**: 2025-10-19  
**Build**: ✅ Passing
