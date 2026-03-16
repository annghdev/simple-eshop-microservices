var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.APIGateway>("apigateway");

builder.Build().Run();
