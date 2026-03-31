using Kernel;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationEvents;
using Payment.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Order.Messaging;

public static class PaymentSucceededHandler
{
    public static async Task Handle(
        PaymentSuceeeded evt,
        OrderDbContext db,
        IMessageBus bus)
    {
        var order = await db.Orders
            .Include(o => o.Logs)
            .FirstOrDefaultAsync(o => o.Id == evt.OrderId)
            ?? throw new NotFoundException($"Order with ID {evt.OrderId} not found.");

        order.MarkOnlinePaid();
        await bus.PublishAsync(new OrderConfirmed(order.Id));
    }
}
