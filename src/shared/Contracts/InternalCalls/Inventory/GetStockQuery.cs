namespace Contracts.InternalCalls.Inventory;

public record GetProductStockQuery(IEnumerable<InventoryItemQuery> QueryItems);
public record InventoryItemQuery(Guid ProductId, Guid? VariantId);
public class ProductStockInfo
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }

    public IEnumerable<ItemStockInfo> StockInfos { get; set; } = [];
}
public class ItemStockInfo
{
    public Guid ItemId { get; set; }
    public int Available { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal WarehouseLat { get; set; }
    public decimal WarehouseLng { get; set; }
}
