using System.ComponentModel.DataAnnotations;

namespace Shipping.Domain;

public class ShipmentTracking
{
    [Key]
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public ShippingStatus Status { get; set; }
}
