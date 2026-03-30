using Contracts.Protos.InventoryStocks;
using Contracts.Protos.OrderReservation;
using Inventory.API.GrpcServices.Callers;
using Inventory.GrpcServices;
using Inventory.IntegrationEvents;
using Inventory.Persistence;
using JasperFx;
using Kernel.Middlewares;
using Marten.Events.Projections;
using Scalar.AspNetCore;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

#region Wolverine + Marten

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();

    opts.Policies.AutoApplyTransactions();

    // Outbox
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.Policies
        .OnException<ConcurrencyException>()
        .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
        .Then.MoveToErrorQueue();

    opts.Policies.ConventionalLocalRoutingIsAdditive();

    opts.AutoBuildMessageStorageOnStartup = AutoCreate.CreateOrUpdate;

    // Local Queue config
    opts.MessagePartitioning
        .ByMessage<IReservationEvent>(x => x.OrderId.ToString())
        .PublishToPartitionedLocalMessaging("reservation", 4, topology =>
        {
            topology.MessagesImplementing<IReservationEvent>();

            topology.MaxDegreeOfParallelism = PartitionSlots.Five;

            topology.ConfigureQueues(queue =>
            {
                queue.TelemetryEnabled(true);
            });
        });

    //opts.Publish(rule =>
    //{
    //    rule.MessagesImplementing<IDomainEvent>();
    //    rule.ToLocalQueue("domain_events").Sequential();
    //});

    // RabbitMQ config
    opts.UseRabbitMq(builder.Configuration.GetConnectionString("rabbitmq")!)
       .AutoProvision()
       .BindExchange("integration_events")
       .ToQueue("inventory.integration_events")
       .ConfigureChannelCreation(c =>
       {
           c.PublisherConfirmationsEnabled = true;
           c.PublisherConfirmationTrackingEnabled = true;
           c.ConsumerDispatchConcurrency = 5;
       });

    opts.ListenToRabbitQueue("inventory.integration_events");

    opts.Publish(rule =>
    {
        rule.MessagesImplementing<IInventoryIntegrationEvent>();

        rule.ToRabbitExchange("integration_events", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Fanout;
            exchange.IsDurable = true;
        });
    });
});

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("inventorydb")!);
    opts.Projections.Snapshot<InventoryItem>(SnapshotLifecycle.Inline);
})
.IntegrateWithWolverine();

builder.Services.AddWolverineHttp();

#endregion

#region Grpc
builder.Services.AddGrpc();

builder.Services
    .AddGrpcClient<OrderReservationItemsGrpc.OrderReservationItemsGrpcClient>(options =>
    {
        options.Address = new Uri(builder.Configuration.GetConnectionString("order") ?? "https://localhost:7003");
    })
    .AddServiceDiscovery();
builder.Services.AddScoped<IGetOrderReservationItemsCaller, GetOrderReservationItemsCaller>();
#endregion

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<InventoryStockGrpcHandler>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    //app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionHandler>();

app.MapWolverineEndpoints();

app.MapGet("/", () => Results.Redirect("scalar/v1"));

using var scope = app.Services.CreateScope();
try
{
    // Seed data
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during database migration or seeding: {ex.Message}");
    throw;
}



return await app.RunJasperFxCommands(args);
