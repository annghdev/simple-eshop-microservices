namespace Inventory.Features.Commands;

public record RestockCommand(Guid Id, int Quantity, Guid OrderId);