using Contracts.Protos.InventoryStocks;
using Contracts.Protos.OrderReservation;
using Contracts.Common;
using Inventory.API.GrpcServices.Callers;
using Inventory.GrpcServices;
using Inventory.IntegrationEvents;
using Inventory.Persistence;
using JasperFx;
using Kernel.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Marten.Events.Projections;
using Scalar.AspNetCore;
using System.Text;
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
    var rabbit = opts.UseRabbitMq(builder.Configuration.GetConnectionString("rabbitmq")!)
       .AutoProvision()
       .ConfigureChannelCreation(c =>
       {
           c.PublisherConfirmationsEnabled = true;
           c.PublisherConfirmationTrackingEnabled = true;
           c.ConsumerDispatchConcurrency = 5;
       });

    rabbit.BindExchange("catalog.exchange")
        .ToQueue("inventory.queue");

    rabbit.BindExchange("order.exchange")
        .ToQueue("inventory.queue");

    opts.ListenToRabbitQueue("inventory.queue");

    opts.Publish(rule =>
    {
        rule.MessagesImplementing<IInventoryIntegrationEvent>();

        rule.ToRabbitExchange("inventory.exchange", exchange =>
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

var jwtIssuer = builder.Configuration["Auth:Jwt:Issuer"] ?? "eshop.gateway";
var jwtAudience = builder.Configuration["Auth:Jwt:Audience"] ?? "eshop.api";
var jwtSigningKey = builder.Configuration["Auth:Jwt:SigningKey"] ?? "dev-signing-key-at-least-32-characters-long";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReadInventory",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Inventory.Read));
    options.AddPolicy("CanAdjustInventory",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Inventory.Adjust));
    options.AddPolicy("CanTransferInventory",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Inventory.Transfer));
    options.AddPolicy("CanReceiveInventory",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Inventory.Receive));
});

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

app.UseAuthentication();
app.UseAuthorization();
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
