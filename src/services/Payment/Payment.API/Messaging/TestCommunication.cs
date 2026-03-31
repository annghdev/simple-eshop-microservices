using Order.IntegrationEvents;
using Payment.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Payment.Messaging;

public class TestCommunicationEndpoint
{
    [WolverinePost("payments/test/communication")]
    public static async Task<IResult> Post(IMessageBus bus)
    {
        await bus.PublishAsync(new PaymentEventPublished());
        return Results.Ok();
    }
}

public static class OrderPublishedHandler
{
    public static async Task Handle(OrderEventPublished message)
    {
        await Task.Delay(100);
    }
}
