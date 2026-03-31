using Inventory.IntegrationEvents;
using Order.Features.Orders;
using Wolverine;

namespace Order.Messaging;

public static class InventoryReservationFailedHandler
{
    public static async Task Handle(InventoryReservationFailed evt, IMessageBus bus)
    {
        await bus.SendAsync(new CancelOrderCommand(
            evt.OrderId,
            "Inventory reservation failed",
            "System"));
    }
}
