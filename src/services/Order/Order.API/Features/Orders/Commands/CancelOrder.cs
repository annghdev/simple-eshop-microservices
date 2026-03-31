using Contracts.Common;
using Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationEvents;
using Order.Security;
using System.Security.Claims;
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
    [Authorize]
    public static async Task<IResult> Put(Guid id, string reason, ClaimsPrincipal user, OrderDbContext db, IMessageBus bus, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return Results.NotFound();
        }

        var canCancelOwn = OrderAuthorizationHelpers.HasPermission(user, PermissionConstants.Order.CancelOwn);
        var customerId = OrderAuthorizationHelpers.GetCustomerId(user);
        if (!canCancelOwn || !customerId.HasValue || order.CustomerId != customerId.Value)
        {
            return Results.Forbid();
        }

        var cancelBy = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var cmd = new CancelOrderCommand(id, reason, cancelBy);
        await bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
}
