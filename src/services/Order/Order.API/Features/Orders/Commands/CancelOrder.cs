using Kernel;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Order.Features.Orders;

public record CancelOrderCommand(Guid OrderId, string Reason, string CancelBy);
public static class CancelOrderHandler
{
    public static async Task Handle(
        CancelOrderCommand cmd,
        OrderDbContext db,
        IMessageBus bus)
    {
        var order = await db.Orders
            .Include(o => o.Logs)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId)
            ?? throw new NotFoundException($"Order with ID {cmd.OrderId} not found.");
        var statusBefore = order.Status;
        order.Cancel(cmd.Reason, cmd.CancelBy);

        if (statusBefore == Domain.OrderStatus.Placed)
        {
            await bus.PublishAsync(new OrderCancelledBeforeConfirm(order.Id));
        }
        else
        {
            await bus.PublishAsync(new OrderCancelledAfterConfirm(order.Id));
        }
    }
}

public static class CancelOrderEndpoint
{
    [WolverinePut("/orders/{id}/cancel")]
    public static IResult Put(Guid id, CancelOrderCommand cmd, IMessageBus bus, CancellationToken ct)
    {
        bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
}
