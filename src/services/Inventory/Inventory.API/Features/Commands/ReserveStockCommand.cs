namespace Inventory.Features;

public record ReserveStockCommand(Guid Id, int Quantity, Guid OrderId);
