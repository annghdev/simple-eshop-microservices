using Microsoft.EntityFrameworkCore;
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
    public static async Task<Domain.Order?> Get(Guid id, IMessageBus bus)
    {
        return await bus.InvokeAsync<Domain.Order?>(new GetOrderQuery(id));
    }
}
