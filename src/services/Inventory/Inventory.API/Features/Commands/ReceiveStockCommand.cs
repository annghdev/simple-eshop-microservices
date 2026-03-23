namespace Inventory.Features.Commands;

public record ReceiveStockCommand(Guid Id, int Quantity);