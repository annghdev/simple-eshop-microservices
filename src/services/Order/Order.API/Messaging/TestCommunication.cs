using Inventory.IntegrationEvents;
using Order.IntegrationEvents;
using Payment.IntegrationEvents;
using Shipping.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Order.Messaging;

public class TestCommunicationEndpoint
{
    [WolverinePost("orders/test/communication")]
    public static async Task<IResult> Post(IMessageBus bus)
    {
        await bus.PublishAsync(new OrderEventPublished());
        return Results.Ok();
    }
}

public static class PaymentPublishedHandler
{
    public static async Task Handle(PaymentEventPublished message)
    {
        await Task.Delay(100);
    }
}

public static class InventoryPublishedHandler
{
    public static async Task Handle(InventoryEventPublished message)
    {
        await Task.Delay(100);
    }
}

public static class ShippingPublishedHandler
{
    public static async Task Handle(ShippingEventPublished message)
    {
        await Task.Delay(100);
    }
}

