var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Resources
var consul = builder.AddContainer("consul", "hashicorp/consul", "1.19")
    .WithHttpEndpoint(port: 8500, targetPort: 8500, name: "ui")
    .WithEndpoint(port: 8600, targetPort: 8600, name: "dns", scheme: "udp")
    .WithArgs("agent", "-server", "-ui", "-node=server-1", "-bootstrap-expect=1", "-client=0.0.0.0")
    .WithEnvironment("CONSUL_BIND_INTERFACE", "eth0")
    .WithEnvironment("CONSUL_CLIENT_INTERFACE", "eth0")
    .WithVolume("consul-data", "/consul/data")
    .WithVolume("consul-config", "/consul/config");

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16-alpine")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithVolume("postgres-data", "/var/lib/postgresql/data")
    .WithBindMount("../../scripts", "/docker-entrypoint-initdb.d");

// pgAdmin for PostgreSQL administration
var pgAdmin = builder.AddContainer("pgadmin", "dpage/pgadmin4", "latest")
    .WithHttpEndpoint(port: 5050, targetPort: 80, name: "web")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@shopnet.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin123")
    .WithEnvironment("PGADMIN_CONFIG_SERVER_MODE", "False")
    .WithEnvironment("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False")
    .WithReference(postgres)
    .WithVolume("pgadmin-data", "/var/lib/pgadmin");

var redis = builder.AddRedis("redis")
    .WithImage("redis", "7-alpine")
    .WithRedisCommander()
    .WithVolume("redis-data", "/data");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
    .WithVolume("rabbitmq-data", "/var/lib/rabbitmq");

var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch", "8.11.0")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http")
    .WithEndpoint(port: 9300, targetPort: 9300, name: "transport")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithEnvironment("bootstrap.memory_lock", "true")
    .WithVolume("elasticsearch-data", "/usr/share/elasticsearch/data");

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithVolume("minio-data", "/data");

// Observability Resources
var seq = builder.AddContainer("seq", "datalust/seq", "latest")
    .WithHttpEndpoint(port: 5341, targetPort: 5341, name: "ingestion")
    .WithHttpEndpoint(port: 8081, targetPort: 80, name: "ui")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SEQ_FIRSTRUN_ADMINPASSWORD", "admin123")
    .WithVolume("seq-data", "/data");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "latest")
    .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui")
    .WithHttpEndpoint(port: 14268, targetPort: 14268, name: "collector-http")
    .WithEndpoint(port: 14250, targetPort: 14250, name: "collector-grpc")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithEnvironment("COLLECTOR_ZIPKIN_HOST_PORT", ":9411")
    .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true");

// TODO: Add microservices when they are implemented
// Databases will be created by Entity Framework migrations in each microservice
// var identityService = builder.AddProject<Projects.R2_ShopNet_Identity_API>("identity-api")
//     .WithReference(postgres)
//     .WithReference(consul)
//     .WithReference(redis)
//     .WithReference(rabbitmq);

builder.Build().Run();
