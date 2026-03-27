using Kernel.Models;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Inventory.Features.InventoryItems;

/// <summary>
/// Represents a query for retrieving items with optional filtering, sorting, and pagination parameters.
/// </summary>
/// <remarks>Use this record to specify criteria when requesting a filtered and paginated list of items. All
/// filter and sort parameters are optional; omit them to retrieve all items. Pagination is controlled by the Page and
/// PageSize properties.</remarks>
public record GetItemsQuery
{
    public Guid? ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? OrderBy { get; set; }
    public bool IsDescending { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public static class GetItemsHandler
{
    public static async Task<PagedResult<InventoryItem>> Handle(GetItemsQuery query, IQuerySession session)
    {
        IQueryable<InventoryItem> q = session.Query<InventoryItem>();

        // Find
        if (query.ProductId != null)
            q = q.Where(i => i.ProductId == query.ProductId);

        if (query.VariantId != null)
            q = q.Where(i => i.VariantId == query.VariantId);

        if (query.WarehouseId != null)
            q = q.Where(i => i.WarehouseId == query.WarehouseId);

        // Sort
        if (!string.IsNullOrEmpty(query.OrderBy))
        {
            if (query.OrderBy == "id" && query.IsDescending)
                q = query.IsDescending ? q.OrderByDescending(i => i.Id) : q.OrderBy(i => i.Id);

            else if (query.OrderBy == "product-id")
                q = query.IsDescending ? q.OrderByDescending(i => i.ProductId) : q.OrderBy(i => i.ProductId);

            else if (query.OrderBy == "variant-id")
                q = query.IsDescending ? q.OrderByDescending(i => i.VariantId) : q.OrderBy(i => i.VariantId);

            else if (query.OrderBy == "warehouse-id")
                q = query.IsDescending ? q.OrderByDescending(i => i.WarehouseId) : q.OrderBy(i => i.WarehouseId);
        }
        else
        {
            q = query.IsDescending ? q.OrderByDescending(i => i.Id) : q.OrderBy(i => i.Id);
        }

        // Paging

        int totalItems = await q.CountAsync();

        var items = await q.Skip((query.Page - 1) * query.PageSize)
                         .Take(query.PageSize).ToListAsync();

        return new PagedResult<InventoryItem>(items, query.Page, query.PageSize, totalItems);
    }
}

public static class GetItemsEndpoint
{
    [WolverineGet("inventory/items")]
    public static async Task<PagedResult<InventoryItem>> GetItems(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? variantId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] string? orderBy,
        [FromQuery] bool isDescending,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMessageBus bus)
    {
        var query = new GetItemsQuery
        {
            ProductId = productId,
            VariantId = variantId,
            WarehouseId = warehouseId,
            OrderBy = orderBy,
            IsDescending = isDescending,
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 12 : pageSize
        };

        var result = await bus.InvokeAsync<PagedResult<InventoryItem>>(query);
        return result;
    }

    [WolverineGet("inventory/items/product/{id}")]
    public static async Task<IEnumerable<InventoryItem>> GetByProductId(Guid id, IQuerySession session, CancellationToken ct)
    {
        var items = await session.Query<InventoryItem>()
            .Where(i => i.ProductId == id)
            .ToListAsync(ct);
        return items;
    }

    [WolverineGet("inventory/items/variant/{id}")]
    public static async Task<IEnumerable<InventoryItem>> GetByVariantId(Guid id, IQuerySession session, CancellationToken ct)
    {
        var items = await session.Query<InventoryItem>()
            .Where(i => i.VariantId != null && i.VariantId == id)
            .ToListAsync(ct);
        return items;
    }
}
