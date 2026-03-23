namespace Inventory.Domain;

public record ItemInitialized(Guid Id, Guid ProductId, Guid? VariantId, Guid WarehouseId);

public record StockReceived(Guid Id, int Quantity);
public record StockAdjusted(Guid Id, int Quantity);
public record StockTranfered(Guid Id, int Quantity, Guid ToWarehouseId);

public record StockReserved(Guid Id, int Quantity, Guid OrderId);
public record ReservationFailed(Guid Id, int Quantity, Guid OrderId);
public record StockCommitted(Guid Id, int Quantity, Guid OrderId);
public record StockReleased(Guid Id, int Quantity, Guid OrderId);
public record ReservationRestocked(Guid Id, int Quantity, Guid OrderId);

public record WarehouseItemDeactivated(Guid Id);
public record WarehouseItemReactivated(Guid Id);
public record ProductDeactivated(Guid Id);
public record ProductReactivated(Guid Id);
public record VariantDeactivated(Guid Id);
public record VariantReactivated(Guid Id);
public record LowStockAlertChanged(Guid Id, int LowStockAlert);