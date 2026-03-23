namespace Inventory.Features.Commands;

public record CommitStockCommand(Guid Id, int Quantity, Guid OrderId);