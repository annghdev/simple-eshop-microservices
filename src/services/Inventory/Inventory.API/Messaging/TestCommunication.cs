using Catalog.IntegrationEvents;
using Inventory.IntegrationEvents;
using Order.IntegrationEvents;

namespace Inventory.Messaging;

public class TestCommunicationEndpoint
{
    [WolverinePost("inventory/test/communication")]
    public static async Task<IResult> Post(IMessageBus bus)
    {
        await bus.PublishAsync(new InventoryEventPublished());
        return Results.Ok();
    }
}

public static class CatalogPublishedHandler
{
    public static async Task Handle(CatalogEventPublished message)
    {
        await Task.Delay(100);
    }
}

public static class OrderPublishedHandler
{
    public static async Task Handle(OrderEventPublished message)
    {
        await Task.Delay(100);
    }
}
