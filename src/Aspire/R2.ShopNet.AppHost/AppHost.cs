var builder = DistributedApplication.CreateBuilder(args);
// Add Docker Compose publishing support
// cd /Volumes/Secure/Projects/R2.ShopNet && aspire publish --project src/R2.ShopNet.AppHost/R2.ShopNet.AppHost.csproj --output-path ./docker-compose-output
builder.AddDockerComposeEnvironment("shopnet");

// Infrastructure Resources
var consul = builder.AddContainer("consul", "hashicorp/consul", "1.19")
    .WithHttpEndpoint(port: 8500, targetPort: 8500, name: "http")
    .WithEndpoint(port: 8600, targetPort: 8600, name: "dns", scheme: "udp")
    .WithArgs("agent", "-server", "-ui", "-node=server-1", "-bootstrap-expect=1", "-client=0.0.0.0")
    .WithEnvironment("CONSUL_BIND_INTERFACE", "eth0")
    .WithEnvironment("CONSUL_CLIENT_INTERFACE", "eth0")
    .WithVolume("consul-data", "/consul/data")
    .WithVolume("consul-config", "/consul/config")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16-alpine")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithVolume("postgres-data", "/var/lib/postgresql/data")
    .WithBindMount("../../scripts", "/docker-entrypoint-initdb.d")
    .WithLifetime(ContainerLifetime.Persistent);

// pgAdmin for PostgreSQL administration
var pgAdmin = builder.AddContainer("pgadmin", "dpage/pgadmin4", "latest")
    .WithHttpEndpoint(port: 5050, targetPort: 80, name: "web")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@shopnet.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin123")
    .WithEnvironment("PGADMIN_CONFIG_SERVER_MODE", "False")
    .WithEnvironment("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False")
    .WithReference(postgres)
    .WithVolume("pgadmin-data", "/var/lib/pgadmin")
    .WithLifetime(ContainerLifetime.Persistent);

var redis = builder.AddRedis("redis")
    .WithImage("redis", "7-alpine")
    .WithRedisCommander()
    .WithVolume("redis-data", "/data")
    .WithLifetime(ContainerLifetime.Persistent);

// var rabbitmq = builder.AddRabbitMQ("rabbitmq")
//     .WithManagementPlugin()
//     .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
//     .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
//     .WithVolume("rabbitmq-data", "/var/lib/rabbitmq")
//     .WithLifetime(ContainerLifetime.Persistent);

// var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch", "8.11.0")
//     .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "api")
//     .WithEndpoint(port: 9300, targetPort: 9300, name: "transport")
//     .WithEnvironment("discovery.type", "single-node")
//     .WithEnvironment("xpack.security.enabled", "false")
//     .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
//     .WithEnvironment("bootstrap.memory_lock", "true")
//     .WithVolume("elasticsearch-data", "/usr/share/elasticsearch/data")
//     .WithLifetime(ContainerLifetime.Persistent);

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithHttpsEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpsEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithArgs("server", "/data", "--console-address", ":9001", "--certs-dir", "/root/.minio/certs")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithVolume("minio-data", "/data")
    .WithBindMount("../../../.certs", "/root/.minio/certs", isReadOnly: true)
    .WithLifetime(ContainerLifetime.Persistent);

// // Observability Resources
// var seq = builder.AddContainer("seq", "datalust/seq", "latest")
//     .WithHttpEndpoint(port: 5341, targetPort: 5341, name: "ingestion")
//     .WithHttpEndpoint(port: 8081, targetPort: 80, name: "web")
//     .WithEnvironment("ACCEPT_EULA", "Y")
//     .WithEnvironment("SEQ_FIRSTRUN_ADMINPASSWORD", "admin123")
//     .WithVolume("seq-data", "/data")
//     .WithLifetime(ContainerLifetime.Persistent);

// var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "latest")
//     .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui")
//     .WithHttpEndpoint(port: 14268, targetPort: 14268, name: "collector")
//     .WithEndpoint(port: 14250, targetPort: 14250, name: "grpc")
//     .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
//     .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp")
//     .WithEnvironment("COLLECTOR_ZIPKIN_HOST_PORT", ":9411")
//     .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true")
//     .WithLifetime(ContainerLifetime.Persistent);

// MailDev - SMTP server for development and testing
// Web UI: http://localhost:1080, SMTP: localhost:1025
var maildev = builder.AddContainer("maildev", "maildev/maildev", "latest")
    .WithHttpEndpoint(port: 1080, targetPort: 1080, name: "web")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp")
    .WithLifetime(ContainerLifetime.Persistent);

// Create databases
var identityDb = postgres.AddDatabase("identitydb");
var catalogDb = postgres.AddDatabase("catalogdb");

// Identity Service
// launchSettings.json:
//   - http: http://localhost:5002
//   - https: https://localhost:5003
var identityService = builder.AddProject<Projects.R2_ShopNet_Identity_API>("identity-service", "https")
    .WithReference(identityDb)
    .WithReference(redis)
    // .WithReference(rabbitmq)
    .WithEnvironment("Consul__KeyValue__Address", consul.GetEndpoint("http"));  // Use localhost since Identity Service runs outside Docker


// Catalog Service
// launchSettings.json:
//   - http: http://localhost:5004
//   - https: https://localhost:5005
var catalogService = builder.AddProject<Projects.R2_ShopNet_Catalog_API>("catalog-service", "http")
    .WithReference(catalogDb)
    .WithReference(redis)
    // .WithReference(rabbitmq)
    .WaitFor(minio)
    .WithEnvironment("Consul__KeyValue__Address", consul.GetEndpoint("http")) // Use localhost since Identity Service runs outside Docker
    .WithEnvironment("MinIO__Endpoint", "localhost:9000")
    .WithEnvironment("MinIO__AccessKey", "minioadmin")
    .WithEnvironment("MinIO__SecretKey", "minioadmin")
    .WithEnvironment("MinIO__BucketName", "product-images")
    .WithEnvironment("MinIO__UseSSL", "true")
    .WithEnvironment("MinIO__Region", "us-east-1");

// API Gateway (YARP with Consul service discovery)
// launchSettings.json:
//   - http: http://localhost:5001
//   - https: https://localhost:5000
// The gateway acts as a single entry point for all client applications
var gateway = builder.AddProject<Projects.R2_ShopNet_Gateway_API>("api-gateway", "https")
    .WithReference(identityService, "https")  // For health checks and testing
    .WithReference(catalogService, "http")   // For health checks and testing
    .WithEnvironment("Consul__Address", consul.GetEndpoint("http"))
    .WaitFor(identityService)
    .WaitFor(catalogService)
    .WithExternalHttpEndpoints();

// Admin Portal (Angular 20)
// Note: Ensure Node.js is in PATH. If using nvm, run Aspire from terminal with nvm environment.
// Angular environment config uses compile-time values in environment.ts files, not runtime environment variables
// For production, clients should connect through the API Gateway
var adminPortal = builder.AddJavaScriptApp("admin-portal", "../../Web/R2.ShopNet.Web.Admin")
    .WithHttpEndpoint(targetPort: 4200)
    .WaitFor(identityService)
    .WaitFor(catalogService)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints()
    .WithEnvironment("NODE_ENV", "development")
    .WithNpm(install: true)
    .WithRunScript("start");

// Blazor Portal (Customer-facing portal)
// launchSettings.json:
//   - https: https://localhost:5007
// Uses OpenID Connect with Authorization Code Flow + PKCE for authentication
// Hot reload is automatically enabled when running through Aspire in Development mode
var blazorPortal = builder.AddProject<Projects.R2_ShopNet_Web_Portal>("blazor-portal", "https")
    .WaitFor(identityService)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
