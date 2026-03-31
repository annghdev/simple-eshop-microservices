using Contracts.Enums;
using Kernel;
using Order.InternalCalls;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Order.Domain;

public class Order
{
    [Key]
    public Guid Id { get; set; }
    [MaxLength(50)]
    public Guid? CustomerId { get; set; }
    public Guid? GuestId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;
    [MaxLength(400)]
    public string? Promotions { get; set; }
    [MaxLength(16)]
    public string? CouponCode { get; set; }
    [MaxLength(50)]
    public string? CouponName { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal PromotionDiscount => Items.Sum(i => i.PromotionDiscount);
    public decimal CouponDiscount { get; set; }
    public decimal DiscountAmount => PromotionDiscount + CouponDiscount;
    public decimal FinalAmount => OriginalAmount - DiscountAmount;
    [MaxLength(250)]
    public string CustomerNote { get; set; } = string.Empty;
    [MaxLength(250)]
    public string ShopNote { get; set; } = string.Empty;
    [MaxLength(500)]
    public string SystemNote { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? ConfirmBy { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public List<OrderLog> Logs { get; set; } = [];
    public bool Paid { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdated { get; set; }

    public void AddItem(OrderItem item)
    {
        if (Items.Any(i => i.ProductId == item.ProductId && i.VariantId == item.VariantId))
        {
            throw new ArgumentException("Item duplicated");
        }
        Items.Add(item);
    }

    public void RemoveItem(Guid itemId)
    {
        var item = Items.Find(i => i.Id == itemId)
            ?? throw new NotFoundException($"Order Item with ID {itemId} does not exists");
    }

    public void MarkReserved()
    {
        ReservationStatus = ReservationStatus.Reserved;
        var now = DateTimeOffset.UtcNow;

        Logs.Add(new OrderLog
        {
            Action = OrderActions.Place,
            Actor = "System",
            Description = "Reservation success",
            TimeStamp = now,
            StatusBefore = Status,
            StatusAfter = OrderStatus.Placed
        });

        Status = OrderStatus.Placed;
        LastUpdated = now;
    }

    public void MarkReservationFailed(string reason)
    {
        ReservationStatus = ReservationStatus.Failed;
        SystemNote += $"Order cancelled. Reason: {reason}";
        var now = DateTimeOffset.UtcNow;
        Logs.Add(new OrderLog
        {
            Action = OrderActions.Place,
            Actor = "System",
            Description = "Reservation failed",
            TimeStamp = now,
            StatusBefore = Status,
            StatusAfter = OrderStatus.Cancelled
        });

        Status = OrderStatus.Cancelled;
        LastUpdated = now;
    }

    public void ConfirmManually(string confirmBy)
    {
        if (Status != OrderStatus.Placed)
        {
            throw new InvalidOperationException("Only order with Placed status can be confirmed");
        }

        var now = DateTimeOffset.UtcNow;

        Logs.Add(new OrderLog
        {
            Action = OrderActions.Confirm,
            Actor = confirmBy,
            Description = "Order confirmed",
            TimeStamp = now,
            StatusBefore = Status,
            StatusAfter = OrderStatus.Confirmed
        });

        Status = OrderStatus.Confirmed;
        LastUpdated = now;
    }

    public void MarkOnlinePaid()
    {

        if (PaymentMethod == PaymentMethod.Online && Status == OrderStatus.Placed)
        {
            Paid = true;
            var now = DateTimeOffset.UtcNow;

            Logs.Add(new OrderLog
            {
                Action = OrderActions.Pay,
                Actor = "System",
                Description = "Online payment success, then confirm Order",
                TimeStamp = now,
                StatusBefore = Status,
                StatusAfter = OrderStatus.Confirmed
            });

            LastUpdated = now;
        }
        else
        {
            throw new InvalidOperationException("Invalid payment method");
        }
    }

    public void MarkShipped()
    {
        if (Status == OrderStatus.Confirmed)
        {
            var now = DateTimeOffset.UtcNow;

            Logs.Add(new OrderLog
            {
                Action = OrderActions.Ship,
                Actor = "System",
                Description = "Shipping Started",
                TimeStamp = now,
                StatusBefore = Status,
                StatusAfter = OrderStatus.Shipped
            });

            Status = OrderStatus.Shipped;
            LastUpdated = now;
        }
        else
        {
            throw new InvalidOperationException("Can not mark Shipped before Confirm");
        }
    }

    public void MarkDelivered()
    {
        if (Status == OrderStatus.Confirmed)
        {
            var now = DateTimeOffset.UtcNow;
            Logs.Add(new OrderLog
            {
                Action = OrderActions.Ship,
                Actor = "System",
                Description = "Order delivered",
                TimeStamp = now,
                StatusBefore = Status,
                StatusAfter = OrderStatus.Delivered
            });

            Status = OrderStatus.Delivered;

            if (PaymentMethod == PaymentMethod.COD)
                Paid = true;

            LastUpdated = now;
        }
        else
        {
            throw new InvalidOperationException("Can not mark Delivered before Ship");
        }
    }

    public void Cancel(string reason, string cancelBy)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled");
        }
        else if (Status == OrderStatus.Shipped)
        {
            throw new InvalidOperationException("Can not cancel an already shipped order");
        }
        else if (Status == OrderStatus.Delivered && LastUpdated.Value.AddDays(7) < DateTimeOffset.Now)
        {
            throw new InvalidOperationException("Can not cancel an already delivered order more than 7 days ago");
        }

        var now = DateTimeOffset.UtcNow;
        Logs.Add(new OrderLog
        {
            Action = OrderActions.Ship,
            Actor = cancelBy,
            Description = $"Order cancelled by {cancelBy} with reason: {reason}",
            TimeStamp = now,
            StatusBefore = Status,
            StatusAfter = OrderStatus.Cancelled
        });
        Status = OrderStatus.Cancelled;
        LastUpdated = now;
    }
}

public class OrderItem
{
    [Key]
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;
    [JsonIgnore]
    public Guid OrderId { get; set; }
    public decimal UnitPrice { get; set; }
    public int TotalQuantity { get; set; }
    public decimal OriginalAmount => UnitPrice * TotalQuantity;
    public decimal PromotionDiscount { get; set; }
    public decimal Amount => OriginalAmount - PromotionDiscount;
    //[JsonIgnore]
    public List<ItemReservation> Reservations { get; set; } = [];
    public FreeItem? FreeItem { get; set; }
}

public class FreeItem
{
    [Key]
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [JsonIgnore]
    public Guid OrderItemId { get; set; }
}

public class ItemReservation
{
    [Key]
    public Guid Id { get; set; }
    [JsonIgnore]
    public Guid OrderItemId { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
}

public class OrderLog
{
    [Key]
    public Guid Id { get; set; }
    [JsonIgnore]
    public Guid OrderId { get; set; }
    [JsonIgnore]
    public Order? Order { get; set; }
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;
    [MaxLength(150)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset TimeStamp { get; set; }
    public OrderStatus StatusBefore { get; set; }
    public OrderStatus StatusAfter { get; set; }
}

public class OrderActions
{
    public const string Create = nameof(Create);
    public const string Place = nameof(Place);
    public const string Pay = nameof(Pay);
    public const string Ship = nameof(Ship);
    public const string MarkDelvered = nameof(MarkDelvered);
    public const string Cancel = nameof(Cancel);
    public const string Confirm = nameof(Confirm);
}

public enum OrderStatus
{
    Initialized = 0,
    Placed = 1,
    Confirmed = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
