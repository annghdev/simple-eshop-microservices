using Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace Payment.Domain;

public class PaymentTransaction
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? ExternalCode { get; set; }
    public decimal Amount { get; set; }
    public PaymentGateway? Gateway { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public PaymentStatus Status { get; set; }
}

public enum PaymentGateway
{
    VnPay,
    Momo,
    ZaloPay,
    Paypal
}

public enum PaymentStatus
{
    Initialized,
    Cancelled,
    Paid,
    Refunded,
}