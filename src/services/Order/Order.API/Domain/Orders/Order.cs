using Order.InternalCalls;

namespace Order.Domain;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CustomerNote { get; set; } = string.Empty;
    public string ShopNote { get; set; } = string.Empty;
    public string SystemNote { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = [];
    public bool Paid { get; set; }
    public PaymentMedthod PaymentMethod { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public OrderStatus Status { get; set; }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal UnitPrice { get; set; }
    public int TotalQuantity { get; set; }
    public List<Reservation> Reservations { get; set; } = [];
}

public class Reservation
{
    public Guid InventoryItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Initialized = 0,
    Placed = 1,
    Confirmed = 2,
    Shipped = 3,
    Delivered = 4,
    Canceled = 5
}

public enum PaymentMedthod
{
    // After Ship
    COD = 0,

    // Before Ship
    Online = 1,
    BankTranfer = 2,
}