using Contracts.Protos.InventoryStocks;
using Contracts.Common;
using JasperFx;
using JasperFx.Core;
using JasperFx.Resources;
using Kernel.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order;
using Order.Persistence;
using Order.GrpcServices;
using Order.IntegrationEvents;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Order.API.GrpcServices.Handlers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureEshopDockerKestrel(grpcOnDedicatedPort8082: true);

#region Wolverine

builder.Host.UseWolverine(opts =>
{
    var dbConectionString = builder.Configuration.GetConnectionString("orderdb")!;
    opts.PersistMessagesWithPostgresql(dbConectionString);

    opts.Services.AddDbContextWithWolverineIntegration<OrderDbContext>(
        x => x.UseNpgsql(dbConectionString));

    opts.UseFluentValidation();

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.Policies
        .OnException<ConcurrencyException>()
        .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
        .Then.MoveToErrorQueue();

    //opts.Policies.ConventionalLocalRoutingIsAdditive();

    opts.AutoBuildMessageStorageOnStartup = AutoCreate.CreateOrUpdate;

    //opts.Publish(rule =>
    //{
    //    rule.MessagesImplementing<IDomainEvent>();

    //    rule.ToLocalQueue("domain_events").Sequential();
    //});

    var rabbit = opts.UseRabbitMq(builder.Configuration.GetConnectionString("rabbitmq")!)
       .AutoProvision()
       .ConfigureChannelCreation(c =>
       {
           c.PublisherConfirmationsEnabled = true;
           c.PublisherConfirmationTrackingEnabled = true;
           c.ConsumerDispatchConcurrency = 5;
       });

    rabbit.BindExchange("payment.exchange")
        .ToQueue("order.queue");

    rabbit.BindExchange("shipping.exchange")
        .ToQueue("order.queue");

    rabbit.BindExchange("inventory.exchange")
        .ToQueue("order.queue");

    opts.ListenToRabbitQueue("order.queue");

    opts.Publish(rule =>
    {
        rule.MessagesImplementing<IOrderIntegrationEvent>();

        rule.ToRabbitExchange("order.exchange", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Fanout;
            exchange.IsDurable = true;
        });
    });
});

builder.Host.UseResourceSetupOnStartup();

builder.Services.AddWolverineHttp();

#endregion

builder.AddServiceDefaults();

var inventoryGrpcAddress = builder.Configuration.GetConnectionString("inventory") ?? "http://localhost:5002";

#region GRPC
builder.Services.AddGrpc();
builder.Services
    .AddGrpcClient<InventoryStockGrpc.InventoryStockGrpcClient>(options =>
    {
        options.Address = new Uri(inventoryGrpcAddress);
    })
    .AddServiceDiscovery();

builder.Services.AddScoped<IGetProductStocksCaller, GetProductStocksCaller>();
#endregion

builder.Services.AddScoped<DataSeeder>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
    options.AddPolicy("CanCreateOrder",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Order.Create));
    options.AddPolicy("CanReadAllOrders",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Order.ReadAll));
    options.AddPolicy("CanReadOwnOrder",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Order.ReadOwn));
    options.AddPolicy("CanCancelOwnOrder",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Order.CancelOwn));
    options.AddPolicy("CanConfirmOrder",
        p => p.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, PermissionConstants.Order.Confirm));
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<GetOrderReservationItemsGrpcHandler>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionHandler>();

app.MapWolverineEndpoints();
app.MapGet("/", () => Results.Redirect("scalar/v1"));

using var scope = app.Services.CreateScope();
try
{
    // Migrate Orders
    var orderContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await orderContext.Database.MigrateAsync();

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