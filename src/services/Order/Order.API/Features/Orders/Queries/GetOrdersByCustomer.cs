using Microsoft.EntityFrameworkCore;

namespace Order.Features.Orders;

public record GetOrdersByCustomerQuery(Guid CustomerId);
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
