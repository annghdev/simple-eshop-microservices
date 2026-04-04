using Catalog.IntegrationEvents;
using Catalog.Persistence;
using JasperFx;
using JasperFx.Core;
using Kernel.Middlewares;
using Marten;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureEshopDockerKestrel();

#region Wolverine + Marten

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.Policies
        .OnException<ConcurrencyException>()
        .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
        .Then.MoveToErrorQueue();

    opts.Policies.ConventionalLocalRoutingIsAdditive();

    opts.AutoBuildMessageStorageOnStartup = AutoCreate.CreateOrUpdate;

    //opts.Publish(rule =>
    //{
    //    rule.MessagesImplementing<IDomainEvent>();

    //    rule.ToLocalQueue("domain_events").Sequential();
    //});

    opts.UseRabbitMq(builder.Configuration.GetConnectionString("rabbitmq")!)
       .AutoProvision()
       .ConfigureChannelCreation(c =>
       {
           c.PublisherConfirmationsEnabled = true;
           c.PublisherConfirmationTrackingEnabled = true;
           c.ConsumerDispatchConcurrency = 5;
       });

    opts.Publish(rule =>
    {
        rule.MessagesImplementing<ICatalogIntegrationEvent>();

        rule.ToRabbitExchange("catalog.exchange", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Fanout;
            exchange.IsDurable = true;
            exchange.BindQueue("inventory.queue");
        });
    });
});

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("catalogdb")!);
})
.IntegrateWithWolverine();

builder.Services.AddWolverineHttp();

#endregion

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddGrpc();

builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

app.MapDefaultEndpoints();

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