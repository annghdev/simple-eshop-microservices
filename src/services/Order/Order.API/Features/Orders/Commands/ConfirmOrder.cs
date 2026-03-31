using Kernel;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Order.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Order.API.Features;

public record ConfirmOrderCommand(Guid OrderId, string ConfirmBy);
public static class ConfirmOrderHandler
{
    public static async Task Handle(
        ConfirmOrderCommand cmd,
        OrderDbContext db,
        IMessageBus bus,
        CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Logs)
            .FirstOrDefaultAsync(o => o.Id == cmd.OrderId)
            ?? throw new NotFoundException($"Order with ID {cmd.OrderId} not found.");

        order.ConfirmManually(cmd.ConfirmBy);

        await bus.PublishAsync(new OrderConfirmed(order.Id));
    }
}

public static class ConfirmOrderEndpoint
{
    [WolverinePut("/orders/{id}/confirm")]
    [Authorize(Policy = "CanConfirmOrder")]
    public static async Task<IResult> Put(Guid id, ClaimsPrincipal user, IMessageBus bus, CancellationToken ct)
    {
        var userId = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var cmd = new ConfirmOrderCommand(id, userId);
        await bus.InvokeAsync(cmd, ct);
        return Results.Ok();
    }
}