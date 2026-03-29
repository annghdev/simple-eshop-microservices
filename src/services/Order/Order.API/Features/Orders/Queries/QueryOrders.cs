using Kernel.Models;
using Microsoft.EntityFrameworkCore;
using Order.Domain;

namespace Order.Features.Orders;

public class GetOrdersQuery
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public decimal? AmountFrom { get; set; }
    public decimal? AmountTo { get; set; }
    public string? CouponCode { get; set; }
    public string? Promotion { get; set; }
    public OrderStatus? Status { get; set; }
}

public static class GetOrdersHandler
{
    public static async Task<PagedResult<Domain.Order>> Handle(
        GetOrdersQuery query,
        OrderDbContext db,
        CancellationToken ct)
    {
        var ordersQuery = db.Orders.AsNoTracking();

        if (query.CustomerId.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CustomerId == query.CustomerId.Value);

        if (!string.IsNullOrEmpty(query.CustomerName))
            ordersQuery = ordersQuery.Where(o => o.CustomerName.Contains(query.CustomerName));

        if (!string.IsNullOrEmpty(query.PhoneNumber))
            ordersQuery = ordersQuery.Where(o => o.PhoneNumber.Contains(query.PhoneNumber));

        if (!string.IsNullOrEmpty(query.Address))
            ordersQuery = ordersQuery.Where(o => o.Address.Contains(query.Address));

        if (query.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.DateFrom.Value.ToDateTime(TimeOnly.MinValue));

        if (query.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.DateTo.Value.ToDateTime(TimeOnly.MaxValue));

        if (!string.IsNullOrEmpty(query.CouponCode))
            ordersQuery = ordersQuery.Where(o => !string.IsNullOrEmpty(o.CouponCode) && o.CouponCode.Contains(query.CouponCode));

        if (!string.IsNullOrEmpty(query.Promotion))
            ordersQuery = ordersQuery.Where(o => !string.IsNullOrEmpty(o.Promotions) && o.Promotions.Contains(query.Promotion));

        if (query.Status.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Status == query.Status.Value);

        var totalItems = await ordersQuery.CountAsync(ct);

        var ordersIds = await ordersQuery
            .OrderByDescending(o => o.Id)
            .Select(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => ordersIds.Contains(o.Id))
            .Include(o => o.Items)
                .ThenInclude(oi => oi.FreeItem)
            .Include(o => o.Logs)
            .ToListAsync(ct);

        return new PagedResult<Domain.Order>(orders, query.Page, query.PageSize, totalItems);
    }
}
