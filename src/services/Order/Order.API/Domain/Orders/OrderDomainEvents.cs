namespace Order.Domain;

public record OrderInitialized(Order Order); // => Select Warehouses => Build projection and publish OrderPlaced
public record OrderReserveFailed(Order Order); // => CancelOrder & add system note

public record ShopConfirmedOrder(Guid OrderId, string ConfirmBy); // => Mark Confirmed => Publish OrderConfirmed
public record SystemConfirmedOrder(Guid OrderId); // => Mark Paid & Confirmed, add system note => Publish OrderConfirmed
public record OrderCanceledBeforeConfirm(Guid OrderId); // => Mark Canceled, add system note => Publish integration event
public record OrderCanceledAfterConfirm(Guid OrderId); // => Mark Canceled, add system note => Publish integration event
public record OrderShipped(Guid OrderId); // => Mark Shipped
public record OrderDelivered(Guid OrderId); // => Mark Delevered and mark as Paid if payment method is COD