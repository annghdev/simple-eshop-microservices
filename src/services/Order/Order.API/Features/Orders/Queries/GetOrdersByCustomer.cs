using Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Order.Security;
using System.Security.Claims;
using Wolverine;
using Wolverine.Http;

namespace Order.Features.Orders;

public record GetOrdersByCustomerQuery(Guid CustomerId);
public record GetOrdersByGuestQuery(Guid GuestId);
public static class GetOrdersByCustomerHandler
{
    public static async Task<List<Domain.Order>> Handle(
        GetOrdersByCustomerQuery query,
        OrderDbContext db,
        CancellationToken ct)
    {
        var orders = await db.Orders
            .Where(o => o.CustomerId == query.CustomerId)
            .Include(o => o.Items)
                .ThenInclude(oi=>oi.FreeItem)
            .Include(o => o.Logs)
            .OrderByDescending(o => o.Id)
            .ToListAsync(ct);
        return orders;
    }
}

public static class GetOrdersByGuestHandler
{
    public static async Task<List<Domain.Order>> Handle(
        GetOrdersByGuestQuery query,
        OrderDbContext db,
        CancellationToken ct)
    {
        var orders = await db.Orders
            .Where(o => o.GuestId == query.GuestId)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.FreeItem)
            .Include(o => o.Logs)
            .OrderByDescending(o => o.Id)
            .ToListAsync(ct);
        return orders;
    }
}

public static class GetOrdersHistoryEndpoint
{
    [WolverineGet("/orders/history")]
    public static async Task<IResult> Get(ClaimsPrincipal user, HttpContext httpContext, IMessageBus bus)
    {
        if (OrderAuthorizationHelpers.IsAuthenticated(user))
        {
            var canReadAll = OrderAuthorizationHelpers.HasPermission(user, PermissionConstants.Order.ReadAll);
            var canReadOwn = OrderAuthorizationHelpers.HasPermission(user, PermissionConstants.Order.ReadOwn);
            if (!canReadAll && !canReadOwn)
            {
                return Results.Forbid();
            }

            var customerId = OrderAuthorizationHelpers.GetCustomerId(user);
            if (!customerId.HasValue)
            {
                return Results.Forbid();
            }

            var orders = await bus.InvokeAsync<List<Domain.Order>>(new GetOrdersByCustomerQuery(customerId.Value));
            return Results.Ok(orders);
        }

        var guestId = OrderAuthorizationHelpers.GetGuestId(httpContext.Request.Headers);
        if (!guestId.HasValue)
        {
            return Results.BadRequest("Missing required header: X-Guest-Id.");
        }

        var guestOrders = await bus.InvokeAsync<List<Domain.Order>>(new GetOrdersByGuestQuery(guestId.Value));
        return Results.Ok(guestOrders);
    }
}
