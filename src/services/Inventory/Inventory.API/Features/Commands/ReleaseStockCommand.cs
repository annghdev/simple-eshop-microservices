namespace Inventory.Features.Commands;

public record ReleaseStockCommand(Guid Id, int Quantity, Guid OrderId);