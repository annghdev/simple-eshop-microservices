using JasperFx;
using JasperFx.Core;
using JasperFx.Resources;
using Kernel.Middlewares;
using Microsoft.EntityFrameworkCore;
using Payment.IntegrationEvents;
using Payment.Persistence;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureEshopDockerKestrel();

#region Wolverine

builder.Host.UseWolverine(opts =>
{
    var dbConectionString = builder.Configuration.GetConnectionString("paymentdb")!;
    opts.PersistMessagesWithPostgresql(dbConectionString);

    opts.Services.AddDbContextWithWolverineIntegration<PaymentDbContext>(
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

    rabbit.BindExchange("order.exchange")
          .ToQueue("payment.queue");

    opts.ListenToRabbitQueue("payment.queue");

    opts.Publish(rule =>
    {
        rule.MessagesImplementing<IPaymentIntegrationEvent>();

        rule.ToRabbitExchange("payment.exchange", exchange =>
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

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMiddleware<GlobalExceptionHandler>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();
app.MapWolverineEndpoints();
app.MapGet("/", () => Results.Redirect("scalar/v1"));

using var scope = app.Services.CreateScope();
try
{
    // Migrate Orders
    var orderContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await orderContext.Database.MigrateAsync();

    // Seed data
    //var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    //await seeder.SeedAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error during database migration or seeding: {ex.Message}");
    throw;
}


return await app.RunJasperFxCommands(args);
