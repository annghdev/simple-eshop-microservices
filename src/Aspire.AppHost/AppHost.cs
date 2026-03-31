using StackExchange.Redis;

var builder = DistributedApplication.CreateBuilder(args);

// support to generate docker-compose.yaml and file .env
builder.AddDockerComposeEnvironment("eshop");

// setup dependencies
const int redisHostPort = 56379;
const int rabbitMqMetricsPort = 15692;
const int rabbitMqManagementPort = 15672;
const int postgresHostPort = 55432;

var postgresUser = builder.AddParameter(
    "postgres-user",
    "postgres",
    publishValueAsDefault: true,
    secret: false);
var postgresPassword = builder.AddParameter(
    "postgres-password",
    "postgres",
    publishValueAsDefault: false,
    secret: true);

var redisPassword = builder.AddParameter(
    "redis-password",
    "redis",
    publishValueAsDefault: false,
    secret: true);

var rabbitMqUser = builder.AddParameter(
    "rabbitmq-user",
    "rabbitmq",
    publishValueAsDefault: true,
    secret: false);
var rabbitMqPassword = builder.AddParameter(
    "rabbitmq-password",
    "rabbitmq",
    publishValueAsDefault: false,
    secret: true);

//var redis = builder.AddRedis("redis", port: redisHostPort, password: redisPassword)
//    .WithEndpointProxySupport(false);

var rabbitMq = builder.AddRabbitMQ("rabbitmq", userName: rabbitMqUser, password: rabbitMqPassword)
    .WithEndpointProxySupport(false)
    .WithManagementPlugin(rabbitMqManagementPort)
    .WithEndpoint(
        targetPort: rabbitMqMetricsPort,
        port: rabbitMqMetricsPort,
        scheme: "http",
        name: "metrics",
        isProxied: false);

var postgres = builder.AddPostgres(
        "postgres",
        userName: postgresUser,
        password: postgresPassword,
        port: postgresHostPort)
    .WithEndpointProxySupport(false)
    .WithPgWeb(pg=>pg.WithHostPort(5050));

//var commonDb = postgres.AddDatabase("commondb");
var catalogDb = postgres.AddDatabase("catalogdb");
var inventoryDb = postgres.AddDatabase("inventorydb");
var orderDb = postgres.AddDatabase("orderdb");
var paymentDb = postgres.AddDatabase("paymentdb");
var shippingDb = postgres.AddDatabase("shippingdb");

const string otlpEndpoint = "http://localhost:4317";
const string tempoOtlpEndpoint = "http://localhost:4319";
const string lokiEndpoint = "http://localhost:3100";


//Projects
var catalog = builder.AddProject<Projects.Catalog_API>("catalog")
    .WithReference(catalogDb).WaitFor(catalogDb)
    //.WithReference(redis).WaitFor(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithEndpoint("http", endpoint => endpoint.Port = 5001);
//.WithEnvironment("JAEGER_OTLP_ENDPOINT", otlpEndpoint)
//.WithEnvironment("TEMPO_OTLP_ENDPOINT", tempoOtlpEndpoint)
//.WithEnvironment("LOKI_ENDPOINT", lokiEndpoint)



var inventory = builder.AddProject<Projects.Inventory_API>("inventory")
    .WithEndpoint("http", endpoint => endpoint.Port = 5002)
    //.WithEnvironment("JAEGER_OTLP_ENDPOINT", otlpEndpoint)
    //.WithEnvironment("TEMPO_OTLP_ENDPOINT", tempoOtlpEndpoint)
    //.WithEnvironment("LOKI_ENDPOINT", lokiEndpoint);
    .WithReference(inventoryDb).WaitFor(inventoryDb)
    .WithReference(rabbitMq).WaitFor(rabbitMq);
//.WithReference(redis).WaitFor(redis)
//.WithReference(rabbitMq).WaitFor(rabbitMq);

var order = builder.AddProject<Projects.Order_API>("order")
    //.WithReference(redis).WaitFor(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5003);
//.WithEnvironment("JAEGER_OTLP_ENDPOINT", otlpEndpoint)
//.WithEnvironment("TEMPO_OTLP_ENDPOINT", tempoOtlpEndpoint)
//.WithEnvironment("LOKI_ENDPOINT", lokiEndpoint)

var payment = builder.AddProject<Projects.Payment_API>("payment")
    //.WithReference(redis).WaitFor(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(paymentDb).WaitFor(paymentDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5004);
//.WithEnvironment("JAEGER_OTLP_ENDPOINT", otlpEndpoint)
//.WithEnvironment("TEMPO_OTLP_ENDPOINT", tempoOtlpEndpoint)
//.WithEnvironment("LOKI_ENDPOINT", lokiEndpoint)

var shipping = builder.AddProject<Projects.Shipping_API>("shipping")
    //.WithReference(redis).WaitFor(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(shippingDb).WaitFor(shippingDb)
    .WithEndpoint("http", endpoint => endpoint.Port = 5005);
//.WithEnvironment("JAEGER_OTLP_ENDPOINT", otlpEndpoint)
//.WithEnvironment("TEMPO_OTLP_ENDPOINT", tempoOtlpEndpoint)
//.WithEnvironment("LOKI_ENDPOINT", lokiEndpoint)

//var gateway = builder.AddProject<Projects.APIGateway>("apigateway")
//    .WithReference(redis)
//    .WithReference(rabbitMq)
//    .WithReference(commonDb)
//    .WithReference(catalog)
//    .WithReference(inventory)
//    .WithReference(order)
//    .WithEndpoint("http", endpoint => endpoint.Port = 5000)
//    .WithEnvironment("JAEGER_OTLP_ENDPOINT", otlpEndpoint)
//    .WithEnvironment("TEMPO_OTLP_ENDPOINT", tempoOtlpEndpoint)
//    .WithEnvironment("LOKI_ENDPOINT", lokiEndpoint)
//    .WaitFor(commonDb)
//    .WaitFor(redis)
//    .WaitFor(rabbitMq);

builder.Build().Run();
