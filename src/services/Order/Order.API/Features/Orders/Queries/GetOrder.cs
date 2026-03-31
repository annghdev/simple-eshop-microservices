using Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Order.Security;
using System.Security.Claims;
using Wolverine;
using Wolverine.Http;

namespace Order.Features.Orders;

public record GetOrderQuery(Guid Id);
public static class GetOrder
{
    public static async Task<Domain.Order?> Handle(GetOrderQuery query, OrderDbContext db)
    {
        var order = await db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi=>oi.FreeItem)
            .Include(o => o.Logs)
            .FirstOrDefaultAsync(o => o.Id == query.Id);
        return order;
    }
}

public static class GetOrderEndpoint
{
    [WolverineGet("/orders/{id}")]
    public static async Task<IResult> Get(Guid id, IMessageBus bus, ClaimsPrincipal user, HttpContext httpContext)
    {
        var order = await bus.InvokeAsync<Domain.Order?>(new GetOrderQuery(id));
        if (order is null)
        {
            return Results.NotFound();
        }

        if (OrderAuthorizationHelpers.IsAuthenticated(user))
        {
            if (OrderAuthorizationHelpers.HasPermission(user, PermissionConstants.Order.ReadAll))
            {
                return Results.Ok(order);
            }

            if (!OrderAuthorizationHelpers.HasPermission(user, PermissionConstants.Order.ReadOwn))
            {
                return Results.Forbid();
            }

            var customerId = OrderAuthorizationHelpers.GetCustomerId(user);
            if (!customerId.HasValue || order.CustomerId != customerId.Value)
            {
                return Results.Forbid();
            }

            return Results.Ok(order);
        }

        var guestId = OrderAuthorizationHelpers.GetGuestId(httpContext.Request.Headers);
        if (!guestId.HasValue || order.GuestId != guestId.Value)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(order);
    }
}
