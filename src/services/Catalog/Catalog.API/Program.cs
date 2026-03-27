using Catalog.IntegrationEvents;
using JasperFx;
using JasperFx.Core;
using Kernel.Interfaces;
using Marten;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

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

        rule.ToRabbitExchange("integration_events", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Fanout;
            exchange.IsDurable = true;
            exchange.BindQueue("inventory.integration_events");
        });
    });
});

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("catalogdb")!);
})
.IntegrateWithWolverine();

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddWolverineHttp();

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

app.MapWolverineEndpoints();

app.MapGet("/", () => Results.Redirect("scalar/v1"));

return await app.RunJasperFxCommands(args);