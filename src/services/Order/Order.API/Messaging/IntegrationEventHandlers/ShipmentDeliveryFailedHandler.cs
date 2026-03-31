using Kernel;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationEvents;
using Shipping.IntegrationEvents;
using Wolverine;

namespace Order.Messaging;

public static class ShipmentDeliveryFailedHandler
{
    public static async Task Handle(ShipmentDeliveryFailed evt, IMessageBus bus, OrderDbContext db)
    {
        var order = await db.Orders
            .Include(o => o.Logs)
            .FirstOrDefaultAsync(o => o.Id == evt.OrderId)
            ?? throw new NotFoundException($"Order with ID {evt.OrderId} not found");

        order.Cancel($"Delivery failed", "System");

        await bus.PublishAsync(new OrderCancelledAfterConfirm(order.Id));
    }
}