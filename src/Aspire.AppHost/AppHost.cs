var builder = DistributedApplication.CreateBuilder(args);

// setup dependencies
//var docker = builder.AddDockerComposeEnvironment("eshop");

var redis = builder.AddRedis("redis");

var rabbitMq = builder.AddRabbitMQ("rabbitmq");

var postgres = builder.AddPostgres("postgres").WithPgWeb();

var commonDb = postgres.AddDatabase("commondb");
var catalogDb = postgres.AddDatabase("catalogdb");
var inventoryDb = postgres.AddDatabase("inventorydb");
var orderDb = postgres.AddDatabase("orderdb");


//Projects
var catalog = builder.AddProject<Projects.Catalog_API>("catalog")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);


var inventory = builder.AddProject<Projects.Inventory_API>("inventory")
    .WithReference(rabbitMq)
    .WithReference(redis)
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

var order = builder.AddProject<Projects.Order_API>("order")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

var gateway = builder.AddProject<Projects.APIGateway>("apigateway")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithReference(commonDb)
    .WaitFor(commonDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

builder.Build().Run();
