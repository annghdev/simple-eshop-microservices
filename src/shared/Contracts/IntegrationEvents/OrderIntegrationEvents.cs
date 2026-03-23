namespace Order.IntegrationEvents;

public record OrderPlaced(
    Guid OrderId,
    decimal Amount,
    List<ItemReservation> Items
    ); // ==> Reserve Inventory and Init Payment
public record ItemReservation(Guid InventoryItemId, int Quantity);
public record OrderConfirmed(Guid OrderId); // ==> Commit stock
public record OrderCancelledBeforeConfirm(Guid OrderId); // ==> Release stock
public record OrderCancelledAfterConfirm(Guid OrderId); // ==> Restock
