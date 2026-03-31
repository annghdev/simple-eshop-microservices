using Order.IntegrationEvents;
using Shipping.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Shipping.Messaging;

public class TestCommunicationEndpoint
{
    [WolverinePost("shippings/test/communication")]
    public static async Task<IResult> Post(IMessageBus bus)
    {
        await bus.PublishAsync(new ShippingEventPublished());
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
