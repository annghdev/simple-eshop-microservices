using System.ComponentModel.DataAnnotations;

namespace Shipping.Domain;

public class Shipment
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int TotalItems { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Depth { get; set; }
    public decimal Weight { get; set; }
    public string PickupAt { get; set; } = string.Empty;
    public decimal ShipFee { get; set; }
    public ShippingStatus Status { get; set; }
}

public class ShippingLog
{
    [Key]
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public ShippingStatus Status { get; set; }
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    public string Description { get; set; } = string.Empty;
}

public enum ShippingStatus
{
    Preparing,
    Shipped,
    Delivered,
    Rejected,
    Returned
}