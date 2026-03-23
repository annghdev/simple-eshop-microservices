namespace Inventory.Domain;

public class InventoryItem
{
    public InventoryItem() { }

    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Available { get; set; }
    public int Reserved { get; set; }
    public int LowStockAlert { get; set; }
    public bool IsWarehouseActive { get; set; } = true;
    public bool IsProductActive { get; set; } = true;
    public bool IsVariantActive { get; set; } = true;
    public bool IsActive => IsWarehouseActive && IsProductActive && IsVariantActive;

    public static ItemInitialized Create(Guid productId, Guid? variantId, Guid warehouseId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Invalid product ID");

        if (variantId != null && variantId == Guid.Empty)
            throw new ArgumentException("Invalid variant ID");

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Invalid warehouse ID");

        return new ItemInitialized(Guid.CreateVersion7(), productId, variantId, warehouseId);
    }

    public void Apply(ItemInitialized e)
    {
        Id = e.Id;
        ProductId = e.ProductId;
        VariantId = e.VariantId;
        WarehouseId = e.WarehouseId;
        IsWarehouseActive = true;
        IsProductActive = true;
        IsVariantActive = true;
    }

    public void Apply(StockReceived e)
    {
        Available += e.Quantity;
    }

    public StockTranfered Tranfer(int quantity, Guid toWarehouseId)
    {
        if (Available < quantity)
            throw new InvalidOperationException("Available quantity not enough");

        if (toWarehouseId == Guid.Empty)
            throw new ArgumentException("Invalid Warehouse ID");

        return new StockTranfered(Id, quantity, toWarehouseId);
    }

    public void Apply(StockTranfered e)
    {
        Available -= e.Quantity;
    }

    public StockAdjusted AdjustStock(int quantity)
    {
        if (IsProductActive && IsVariantActive)
            throw new InvalidOperationException("The product was not locked before the adjustment, that may lead to errors in inventory data");

        return new StockAdjusted(Id, quantity);
    }

    public void Apply(StockAdjusted e)
    {
        Available = e.Quantity;
    }

    public void Apply(StockReserved e)
    {
        Available -= e.Quantity;
        Reserved += e.Quantity;
    }

    public void Apply(StockCommitted e)
    {
        Reserved -= e.Quantity;
    }

    public void Apply(StockReleased e)
    {
        Reserved -= e.Quantity;
        Available += e.Quantity;
    }

    public void Apply(ReservationRestocked e)
    {
        Available += e.Quantity;
    }

    public WarehouseItemDeactivated DeactiveWarehouseItem()
    {
        return new WarehouseItemDeactivated(Id);
    }

    public void Apply(WarehouseItemDeactivated e)
    {
        IsWarehouseActive = false;
    }

    public WarehouseItemReactivated ReactiveWarehouseItem()
    {
        return new WarehouseItemReactivated(Id);
    }

    public void Apply(WarehouseItemReactivated e)
    {
        IsWarehouseActive = true;
    }

    public ProductDeactivated DeactiveProduct()
    {
        return new ProductDeactivated(Id);
    }

    public void Apply(ProductDeactivated e)
    {
        IsProductActive = false;
    }

    public ProductReactivated ReactiveProduct()
    {
        return new ProductReactivated(Id);
    }

    public void Apply(ProductReactivated e)
    {
        IsProductActive = true;
    }


    public VariantDeactivated DeactiveVariant()
    {
        return new VariantDeactivated(Id);
    }

    public void Apply(VariantDeactivated e)
    {
        IsProductActive = false;
    }

    public VariantReactivated ReactiveVariant()
    {
        return new VariantReactivated(Id);
    }

    public void Apply(VariantReactivated e)
    {
        IsProductActive = true;
    }

    public LowStockAlertChanged ChangeLowStockAlert(int lowStockAlert)
    {
        if (lowStockAlert < 0)
            throw new ArgumentException("Low stock alert quantity can not be negative");

        return new LowStockAlertChanged(Id, lowStockAlert);
    }

    public void Apply(LowStockAlertChanged e)
    {
        LowStockAlert = e.LowStockAlert;
    }
}
