using Kernel;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationEvents;
using Payment.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Order.Messaging;

public static class PaymentFailedHandler
{
    public static async Task Handle(
        PaymentFailed evt,
        OrderDbContext db,
        IMessageBus bus)
    {
        var order = await db.Orders
            .Include(o=>o.Logs)
            .FirstOrDefaultAsync(o=>o.Id == evt.OrderId)
            ?? throw new NotFoundException($"Order with ID {evt.OrderId} not found.");
  
        order.Cancel("Payment failed", "System");
        await bus.PublishAsync(new OrderCancelledBeforeConfirm(order.Id));
    }
}
