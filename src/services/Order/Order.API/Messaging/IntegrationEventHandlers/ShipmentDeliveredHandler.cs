using Kernel;
using Microsoft.EntityFrameworkCore;
using Shipping.IntegrationEvents;

namespace Order.Messaging;

public static class ShipmentDeliveredHandler
{
    public static async Task Handle(
        ShipmentDelivered evt,
        OrderDbContext db)
    {
        var order = await db.Orders
            .Include(o=>o.Logs)
            .FirstOrDefaultAsync(o=>o.Id == evt.OrderId)
            ?? throw new NotFoundException($"Order with ID {evt.OrderId} not found.");
   
        order.MarkDelivered();
    }
}
