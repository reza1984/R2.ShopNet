# R2.ShopNet - E-Commerce Microservices Platform

A comprehensive e-commerce platform built with .NET 9, Angular 20, and microservices architecture.

## Overview

R2.ShopNet is a full-featured e-commerce solution consisting of 13 microservices and 4 web applications:

### Microservices
- **Identity Service** - User authentication and management (OpenIddict)
- **Authorization Service** - Role-based access control and permissions
- **Catalog Service** - Product and category management
- **Inventory Service** - Stock management and warehouse operations
- **Search Service** - Elasticsearch-powered product search
- **Cart Service** - Shopping cart management
- **Orders Service** - Order processing and tracking
- **Payment Service** - Payment gateway integration
- **Delivery Service** - Delivery tracking and route optimization
- **Warehouse Service** - Warehouse and location management
- **Notifications Service** - Email/SMS/Push notifications
- **Analytics Service** - Business intelligence and reporting
- **API Gateway** - YARP-based reverse proxy with service discovery

### Web Applications
- **Shopping App** - Customer-facing e-commerce website (Angular 20)
- **Warehouse App** - Warehouse management system (Angular 20)
- **Delivery App** - Driver mobile app (Angular 20 PWA)
- **Admin Portal** - Administrative dashboard (Angular 20)

## Technology Stack

### Backend
- **.NET 9** - Latest ASP.NET Core
- **PostgreSQL 16** - Primary database
- **Redis 7.x** - Caching layer
- **RabbitMQ 3.x** - Message queue
- **Elasticsearch 8.x** - Full-text search
- **Consul 1.19** - Service discovery and configuration
- **MinIO** - S3-compatible object storage
- **OpenIddict** - OAuth 2.0 / OpenID Connect
- **Entity Framework Core** - ORM
- **YARP** - Reverse proxy / API Gateway

### Frontend
- **Angular 20** - Web framework
- **TypeScript** - Programming language
- **Angular Material / Tailwind CSS** - UI framework
- **RxJS** - Reactive programming
- **Signals** - State management
- **Zoneless** - Change detection strategy

### Infrastructure
- **Docker** - Containerization
- **Docker Compose** - Local orchestration
- **.NET Aspire** - Cloud-native orchestration
- **Consul** - Service discovery
- **Serilog** - Structured logging
- **OpenTelemetry** - Distributed tracing

## Architecture Patterns

- **Clean Architecture** - Domain-driven design with clear boundaries
- **CQRS** - Command Query Responsibility Segregation
- **Event-Driven Architecture** - Loosely coupled services via domain events
- **Microservices** - Independent, scalable services
- **API Gateway Pattern** - Single entry point with service discovery
- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management

## Prerequisites

### Development Environment
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20.x LTS](https://nodejs.org/)
- [Angular CLI 20.x](https://angular.io/cli) - `npm install -g @angular/cli`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Podman](https://podman.io/)
- [Git](https://git-scm.com/)

### Recommended IDEs
- **Backend**: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)
- **Frontend**: [Visual Studio Code](https://code.visualstudio.com/) with Angular extensions

### Database Clients (Optional)
- **PostgreSQL**: [pgAdmin](https://www.pgadmin.org/) or [DBeaver](https://dbeaver.io/)
- **Redis**: [RedisInsight](https://redis.com/redis-enterprise/redis-insight/)
- **API Testing**: [Postman](https://www.postman.com/) or [Insomnia](https://insomnia.rest/)

## Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd openspec
```

### 2. Start Infrastructure Services
```bash
# Start Consul, PostgreSQL, Redis, RabbitMQ, Elasticsearch, and MinIO
docker-compose up -d

# Verify all services are running
docker-compose ps
```

### 3. Verify Consul
Open [http://localhost:8500](http://localhost:8500) to access the Consul UI.

### 4. Build the Solution
```bash
# Restore and build all projects
dotnet restore
dotnet build
```

### 5. Run Database Migrations
```bash
# Run migrations for each service (when implemented)
cd src/Services/Identity/R2.ShopNet.Identity.API
dotnet ef database update

cd ../../../Catalog/R2.ShopNet.Catalog.API
dotnet ef database update

# Repeat for other services...
```

### 6. Run the Services
```bash
# Option 1: Run individual services
cd src/Services/Identity/R2.ShopNet.Identity.API
dotnet run

# Option 2: Use .NET Aspire (when configured)
cd src/R2.ShopNet.AppHost
dotnet run
```

### 7. Run Frontend Applications
```bash
# Shopping App
cd src/Web/R2.ShopNet.Web.Shopping
npm install
ng serve

# Admin Portal
cd src/Web/R2.ShopNet.Web.Admin
npm install
ng serve --port 4201

# Warehouse App
cd src/Web/R2.ShopNet.Web.Warehouse
npm install
ng serve --port 4202

# Delivery App
cd src/Web/R2.ShopNet.Web.Delivery
npm install
ng serve --port 4203
```

## Project Structure

```
openspec/
├── src/
│   ├── Framework/                          # Shared framework libraries
│   │   ├── R2.ShopNet.Framework.Common/
│   │   ├── R2.ShopNet.Framework.CQRS/
│   │   ├── R2.ShopNet.Framework.Events/
│   │   ├── R2.ShopNet.Framework.Validation/
│   │   └── R2.ShopNet.Framework.ServiceDiscovery/
│   ├── Services/                           # Microservices
│   │   ├── Identity/
│   │   ├── Authorization/
│   │   ├── Catalog/
│   │   ├── Inventory/
│   │   ├── Search/
│   │   ├── Cart/
│   │   ├── Orders/
│   │   ├── Payment/
│   │   ├── Delivery/
│   │   ├── Warehouse/
│   │   ├── Notifications/
│   │   └── Analytics/
│   ├── ApiGateway/                         # YARP API Gateway
│   └── Web/                                # Angular Applications
│       ├── R2.ShopNet.Web.Shopping/
│       ├── R2.ShopNet.Web.Admin/
│       ├── R2.ShopNet.Web.Warehouse/
│       └── R2.ShopNet.Web.Delivery/
├── tests/                                  # Test projects
├── docs/                                   # Documentation
├── docker-compose.yml                      # Infrastructure setup
└── R2.ShopNet.sln                         # Solution file
```

## Service Ports

| Service | Port | Description |
|---------|------|-------------|
| Consul | 8500 | Service discovery UI |
| PostgreSQL | 5432 | Database |
| Redis | 6379 | Cache |
| RabbitMQ | 5672, 15672 | Message queue (15672 = UI) |
| Elasticsearch | 9200, 9300 | Search engine |
| MinIO | 9000, 9001 | Object storage (9001 = UI) |
| API Gateway | 5000 | Main entry point |
| Identity Service | 5001 | Authentication |
| Catalog Service | 5002 | Products |
| Shopping App | 4200 | Customer website |
| Admin Portal | 4201 | Administration |
| Warehouse App | 4202 | Warehouse operations |
| Delivery App | 4203 | Driver mobile app |

## Configuration

### Environment Variables
Each service requires the following environment variables:

```bash
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=shopnet_<service>;Username=postgres;Password=postgres

# Redis
Redis__Configuration=localhost:6379

# RabbitMQ
RabbitMQ__Host=localhost
RabbitMQ__Username=guest
RabbitMQ__Password=guest

# Consul
Consul__Address=http://localhost:8500
Consul__ServiceName=<service-name>

# JWT
Jwt__Secret=<your-secret-key>
Jwt__Issuer=R2.ShopNet
Jwt__Audience=R2.ShopNet
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover

# Run specific test project
dotnet test tests/R2.ShopNet.Identity.Tests/
```

## Development Workflow

1. **Branch Strategy**: Use `main` for production-ready code, `develop` for integration
2. **Commit Convention**: Follow [Conventional Commits](https://www.conventionalcommits.org/)
3. **Pull Requests**: Required for all changes to `main` and `develop`
4. **Code Review**: At least one approval required
5. **CI/CD**: Automated build and test on every commit

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and development process.

## Documentation

- [Implementation Checklist](docs/Implementation-Checklist.md) - Detailed task breakdown
- [Architecture Decisions](docs/architecture/) - ADR records
- [API Documentation](docs/api/) - API specifications
- [User Guides](docs/guides/) - End-user documentation

## Support

- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **Email**: support@r2shopnet.com

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Project Status

**Phase**: 0 (Infrastructure Setup) → Phase 1 (Core Backend Services)
**Timeline**: 10 months
**Team Size**: 6-8 developers

### Current Progress
- ✅ Project structure created
- ✅ Framework libraries scaffolded
- ✅ Configuration files created (.gitignore, .editorconfig)
- ⏳ Docker Compose infrastructure setup
- ⏳ Framework implementations
- ⏳ Service implementations

## Authors

- Development Team - R2.ShopNet

## Acknowledgments

- .NET Team for .NET 9
- Angular Team for Angular 20
- HashiCorp for Consul
- All open-source contributors
