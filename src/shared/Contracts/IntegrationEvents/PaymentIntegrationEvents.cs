using Kernel.Interfaces;

namespace Payment.IntegrationEvents;

public interface IPaymentIntegrationEvent : IIntegrationEvent;

public record PaymentSuceeeded(Guid PaymentId, Guid OrderId, decimal Amount) : IPaymentIntegrationEvent; // ==> Confirm Order and mark paid
public record PaymentFailed(Guid PaymentId, Guid OrderId) : IPaymentIntegrationEvent; // ==> Confirm Order and mark paid
