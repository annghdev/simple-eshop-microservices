using Kernel.Interfaces;

namespace Inventory.IntegrationEvents;

public interface IInventoryIntegrationEvent : IIntegrationEvent;

public record InventoryReservationFailed(Guid OrderId): IInventoryIntegrationEvent; // ==> CancelOrder
