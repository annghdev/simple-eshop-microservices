using Kernel.Interfaces;

namespace Inventory.Domain;

public interface IReservationEvent : IDomainEvent
{
    Guid OrderId { get; }
}
public record ItemInitialized(Guid Id, Guid ProductId, Guid? VariantId, Guid WarehouseId) : IDomainEvent;

public record StockReceived(Guid Id, int Quantity);
public record StockAdjusted(Guid Id, int Quantity);
public record StockTransfered(Guid Id, int Quantity, Guid ToWarehouseId) : IDomainEvent;

public record ReservationSucceeded(Guid Id, int Quantity, Guid OrderId) : IReservationEvent;
public record ReservationFailed(Guid Id, int Quantity, Guid OrderId) : IReservationEvent;
public record ReservationCommitted(Guid Id, int Quantity, Guid OrderId) : IReservationEvent;
public record ReservationReleased(Guid Id, int Quantity, Guid OrderId) : IReservationEvent;
public record ReservationRestocked(Guid Id, int Quantity, Guid OrderId) : IReservationEvent;

public record WarehouseItemDeactivated(Guid Id) : IDomainEvent;
public record WarehouseItemReactivated(Guid Id) : IDomainEvent;
public record ProductDeactivated(Guid Id) : IDomainEvent;
public record ProductReactivated(Guid Id) : IDomainEvent;
public record VariantDeactivated(Guid Id) : IDomainEvent;
public record VariantReactivated(Guid Id) : IDomainEvent;
public record LowStockAlertChanged(Guid Id, int LowStockAlert) : IDomainEvent;