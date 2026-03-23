namespace Inventory.Features.Commands;

public record AdjustStockCommand(Guid Id, int Quantity);