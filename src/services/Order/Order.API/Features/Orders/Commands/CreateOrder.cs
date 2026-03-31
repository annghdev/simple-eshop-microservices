using FluentValidation;
using Microsoft.Extensions.Primitives;
using Order.Domain;
using Order.GrpcServices;
using Order.IntegrationEvents;
using Order.InternalCalls;
using Contracts.Protos.InventoryStocks;
using Wolverine;
using Wolverine.Http;
using Contracts.Enums;

namespace Order.Features.Orders;

public class CreateOrderCommand
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? GuestId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CouponCode { get; set; } = string.Empty;
    public string? CouponName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;

    public IEnumerable<OrderItemDto> Items { get; set; } = [];
}
public record OrderItemDto(Guid ProductId, Guid? VariantId, int Quantity);

public static class CreateOrderHandler
{
    public static async Task Handle(
        CreateOrderCommand cmd,
        OrderDbContext context,
        IMessageBus bus,
        IGetProductStocksCaller getProductStocksCaller,
        CancellationToken ct)
    {
        cmd.Id = cmd.Id == Guid.Empty ? Guid.CreateVersion7() : cmd.Id;

        var order = InitializeOrder(cmd); // Step 1
        ApplySimulatedPricingAndPromotion(order); // Step 2
        await ReserveInventoryAsync(order, getProductStocksCaller, ct); // Step 3
        await PublishEvensAsync(order, context, bus, ct); // Step 4
    }

    private static Domain.Order InitializeOrder(CreateOrderCommand cmd)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Domain.Order
        {
            Id = cmd.Id,
            CustomerId = cmd.CustomerId,
            GuestId = cmd.GuestId,
            CustomerName = cmd.CustomerName,
            Address = cmd.Address,
            PhoneNumber = cmd.PhoneNumber,
            CouponCode = cmd.CouponCode,
            CouponName = cmd.CouponName,
            PaymentMethod = cmd.PaymentMethod,
            ReservationStatus = ReservationStatus.Pending,
            Status = OrderStatus.Initialized,
            CreatedAt = now,
            LastUpdated = now
        };

        foreach (var item in cmd.Items)
        {
            order.AddItem(new OrderItem
            {
                Id = Guid.CreateVersion7(),
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ItemName = $"Product-{item.ProductId.ToString()[..8]}",
                TotalQuantity = item.Quantity
            });
        }

        return order;
    }

    private static void ApplySimulatedPricingAndPromotion(Domain.Order order)
    {
        const decimal defaultUnitPrice = 100_000m;
        foreach (var item in order.Items)
        {
            item.UnitPrice = defaultUnitPrice;
            if (item.TotalQuantity >= 3)
            {
                item.PromotionDiscount = item.OriginalAmount * 0.05m;
            }
        }

        order.OriginalAmount = order.Items.Sum(i => i.OriginalAmount);
        if (string.IsNullOrWhiteSpace(order.CouponCode))
        {
            return;
        }

        if (!string.Equals(order.CouponCode, "SALE10", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Coupon is invalid or expired.");
        }

        order.CouponName ??= "10% discount (max 50,000 VND)";
        var amountAfterPromotion = order.OriginalAmount - order.PromotionDiscount;
        order.CouponDiscount = Math.Min(amountAfterPromotion * 0.1m, 50_000m);
    }

    private static async Task ReserveInventoryAsync(Domain.Order order, IGetProductStocksCaller getProductStocksCaller, CancellationToken ct)
    {
        var stockResponse = await getProductStocksCaller.Get(BuildStockRequest(order), ct);

        foreach (var orderItem in order.Items)
        {
            var productStock = FindMatchingProductStock(stockResponse, orderItem);
            if (productStock is null)
            {
                throw new ArgumentException($"Stock data not found for ProductId={orderItem.ProductId}, VariantId={orderItem.VariantId}");
            }

            AllocateReservations(orderItem, productStock);
        }

        order.MarkReserved();
    }

    private static async Task PublishEvensAsync(Domain.Order order, OrderDbContext context, IMessageBus bus, CancellationToken ct)
    {
        context.Orders.Add(order);

        await bus.PublishAsync(new OrderCreated(order));
        await bus.PublishAsync(new OrderPlaced(
            order.Id,
            order.FinalAmount,
            order.PaymentMethod.ToString(),
            order.Items.SelectMany(i => i.Reservations)
                .Select(r => new IntegrationEvents.ItemReservation(r.InventoryItemId, r.Quantity))
                .ToList()));
    }

    private static GetProductStocksRequest BuildStockRequest(Domain.Order order)
    {
        var stockRequest = new GetProductStocksRequest();
        stockRequest.Items.AddRange(order.Items.Select(i => new ProductVariantQuery
        {
            ProductId = i.ProductId.ToString(),
            VariantId = i.VariantId?.ToString() ?? string.Empty
        }));

        return stockRequest;
    }

    private static ProductStock? FindMatchingProductStock(GetProductStocksResponse stockResponse, OrderItem orderItem)
    {
        return stockResponse.Products.FirstOrDefault(p =>
            Guid.TryParse(p.ProductId, out var productId) &&
            productId == orderItem.ProductId &&
            VariantMatches(p.VariantId, orderItem.VariantId));
    }

    private static void AllocateReservations(OrderItem orderItem, ProductStock productStock)
    {
        var remaining = orderItem.TotalQuantity;
        foreach (var stockInfo in productStock.StockInfos
            .Where(s => s.Available > 0)
            .OrderByDescending(s => s.Available))
        {
            if (remaining <= 0)
            {
                break;
            }

            if (!Guid.TryParse(stockInfo.ItemId, out var inventoryItemId) ||
                !Guid.TryParse(stockInfo.WarehouseId, out var warehouseId))
            {
                continue;
            }

            var allocated = Math.Min(remaining, stockInfo.Available);
            orderItem.Reservations.Add(new Domain.ItemReservation
            {
                Id = Guid.CreateVersion7(),
                OrderItemId = orderItem.Id,
                InventoryItemId = inventoryItemId,
                WarehouseId = warehouseId,
                Quantity = allocated
            });
            remaining -= allocated;
        }

        if (remaining > 0)
        {
            throw new ArgumentException($"Insufficient stock for ProductId={orderItem.ProductId}, VariantId={orderItem.VariantId}");
        }
    }

    private static bool VariantMatches(string variantId, Guid? itemVariantId)
    {
        if (string.IsNullOrWhiteSpace(variantId))
        {
            return itemVariantId is null;
        }

        return Guid.TryParse(variantId, out var parsedVariantId) && parsedVariantId == itemVariantId;
    }
}

public static class CreateOrderEndpoint
{
    [WolverinePost("orders")]
    public static async Task<IResult> Post(CreateOrderCommand cmd, HttpContext httpContext, IMessageBus bus)
    {
        cmd.Id = cmd.Id == Guid.Empty ? Guid.CreateVersion7() : cmd.Id;
        cmd.CustomerId = TryGetGuidHeader(httpContext.Request.Headers, "X-Customer-Id", "CustomerId");
        cmd.GuestId = TryGetGuidHeader(httpContext.Request.Headers, "X-Guest-Id", "GuestId");

        if (cmd.CustomerId.HasValue && cmd.GuestId.HasValue)
        {
            return Results.BadRequest("Provide only one header: CustomerId or GuestId.");
        }

        if (!cmd.CustomerId.HasValue && !cmd.GuestId.HasValue)
        {
            return Results.BadRequest("Missing required header: CustomerId or GuestId.");
        }

        await bus.SendAsync(cmd);
        return Results.Accepted($"/orders/{cmd.Id}", new { orderId = cmd.Id });
    }

    private static Guid? TryGetGuidHeader(IHeaderDictionary headers, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!headers.TryGetValue(key, out StringValues value))
            {
                continue;
            }

            var raw = value.FirstOrDefault();
            if (Guid.TryParse(raw, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty();
                item.RuleFor(i => i.Quantity).GreaterThan(0);
            });

        RuleFor(x => x)
            .Must(x => x.CustomerId.HasValue ^ x.GuestId.HasValue)
            .WithMessage("Exactly one of CustomerId or GuestId must be provided.");
    }
}