using Kernel.Interfaces;

namespace Inventory.IntegrationEvents;

public interface IInventoryIntegrationEvent : IIntegrationEvent;
public record InventoryEventPublished(string Message = "Event produced from Inventory Service to Inventory Exchange")
    : IInventoryIntegrationEvent;
public record InventoryReservationFailed(Guid OrderId): IInventoryIntegrationEvent; // ==> CancelOrder
