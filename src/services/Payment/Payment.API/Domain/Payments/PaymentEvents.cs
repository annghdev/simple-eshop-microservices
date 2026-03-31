namespace Payment.Domain;

public record OnlinePaymentInitialized(Guid Id, Guid OrderId);
public record OnlinePaymentSucceeded(Guid Id, Guid OrderId);
public record OnlinePaymentCancelled(Guid Id, Guid OrderId);
public record OnlinePaymentRefunded(Guid Id, Guid OrderId);
public record BankTransferPaymentReceived(Guid Id, Guid OrderId);