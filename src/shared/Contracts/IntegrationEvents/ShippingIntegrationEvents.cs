using Kernel.Interfaces;

namespace Shipping.IntegrationEvents;

public interface IShippingIntegrationEvent : IIntegrationEvent;
public record ShippingStarted(Guid ShipmentId, Guid OrderId, DateTimeOffset StartedAt) : IShippingIntegrationEvent; // ==> Mark order as Shipped
public record ShipmentDelivered(Guid ShipmentId, Guid OrderId, DateTimeOffset DeliveredAt) : IShippingIntegrationEvent; // ==> Mark order as Shipped
public record ShipmentDeliveryFailed(Guid ShipmentId, Guid OrderId, DateTimeOffset OccurredAt) : IShippingIntegrationEvent; // ==> Mark order as Shipped
