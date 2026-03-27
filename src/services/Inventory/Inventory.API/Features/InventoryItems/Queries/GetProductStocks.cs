using Contracts.InternalCalls.Inventory;

namespace Inventory.Features.InventoryItems;

public class GetProductStockHandler
{
    public static async Task<List<ProductStockInfo>> Handle(GetProductStockQuery query, IQuerySession session)
    {
        var result = new List<ProductStockInfo>();

        var warehouses = await session.Query<Warehouse>().ToListAsync() ?? [];

        foreach (var queryItem in query.QueryItems)
        {
            var items = await session.Query<InventoryItem>()
                .Where(i => i.ProductId == queryItem.ProductId && i.VariantId == queryItem.VariantId)
                .ToListAsync();

            result.Add(new ProductStockInfo
            {
                ProductId = queryItem.ProductId,
                VariantId = queryItem.VariantId,
                StockInfos = items.Select(i => new ItemStockInfo
                {
                    ItemId = i.Id,
                    Available = i.Available,
                    WarehouseId = i.WarehouseId,
                    WarehouseLat = 0,
                    WarehouseLng = 0,
                })
            });
        }

        return result;
    }
}
public class GetProductStockEndpoint
{
    [WolverinePost("inventory/stocks/")]
    public static async Task<ProductStockInfo> Get(GetProductStockQuery query, IMessageBus bus)
    {
        var data = await bus.InvokeAsync<ProductStockInfo>(query);
        return data;
    }
}

