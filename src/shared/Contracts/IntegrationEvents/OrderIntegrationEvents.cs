using Kernel.Interfaces;

namespace Order.IntegrationEvents;

public interface IOrderIntegrationEvent : IIntegrationEvent;

public record OrderPlaced(
    Guid OrderId,
    decimal Amount,
    string PaymentMethod,
    List<ItemReservation> Items) : IOrderIntegrationEvent; // ==> Reserve Inventory and Init Payment
public record ItemReservation(Guid InventoryItemId, int Quantity);
public record OrderConfirmed(Guid OrderId) : IOrderIntegrationEvent; // ==> Commit stock
public record OrderCancelledBeforeConfirm(Guid OrderId) : IOrderIntegrationEvent; // ==> Release stock
public record OrderCancelledAfterConfirm(Guid OrderId) : IOrderIntegrationEvent; // ==> Restock
