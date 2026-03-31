using Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Order.Domain;

namespace Order.Persistence;

public class DataSeeder(OrderDbContext context)
{
    private static readonly Guid SeedOrderId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid SeedOrderItemId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid SeedFreeItemId = Guid.Parse("7a7a7a7a-7a7a-7a7a-7a7a-7a7a7a7a7a7a");
    private static readonly Guid SeedOrderLogId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid SeedMainReservationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid SeedFreeItemReservationId = Guid.Parse("9a9a9a9a-9a9a-9a9a-9a9a-9a9a9a9a9a9a");

    private static readonly Guid ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MainVariantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid FreeVariantId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid MainWarehouseId = Guid.Parse("11111111-3333-3333-3333-333333333333");
    private static readonly Guid FreeWarehouseId = Guid.Parse("22222222-3333-3333-3333-333333333333");
    private static readonly Guid MainInventoryItemId = Guid.Parse("11111111-1111-3333-3333-333333333333");
    private static readonly Guid FreeInventoryItemId = Guid.Parse("11111111-2222-3333-3333-333333333333");

    public async Task SeedAsync()
    {
        if (await context.Orders.AnyAsync(x => x.Id == SeedOrderId))
        {
            return;
        }

        var order = new Domain.Order
        {
            Id = SeedOrderId,
            CustomerId = Guid.Parse("11111111-2222-3333-4444-333333333333"),
            Address = "123 Seed Street, Ho Chi Minh City",
            OriginalAmount = 799m,
            CouponDiscount = 0m,
            CustomerNote = "Please call before delivery",
            ShopNote = "Seed order for local development",
            SystemNote = "Seeded from Catalog + Inventory static IDs with free variant item",
            PaymentMethod = PaymentMethod.COD,
            Status = OrderStatus.Placed,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            Items =
            [
                new OrderItem
                {
                    Id = SeedOrderItemId,
                    OrderId = SeedOrderId,
                    ProductId = ProductId,
                    VariantId = MainVariantId,
                    ItemName = "iPhone 15 - Black / 128GB",
                    UnitPrice = 799m,
                    TotalQuantity = 1,
                    PromotionDiscount = 0m,
                    FreeItem = new FreeItem
                    {
                        Id = SeedFreeItemId,
                        OrderItemId = SeedOrderItemId,
                        ProductId = ProductId,
                        VariantId = FreeVariantId,
                        ItemName = "iPhone 15 - Blue / 256GB (Free)",
                        Quantity = 1
                    },
                    Reservations =
                    [
                        new ItemReservation
                        {
                            Id = SeedMainReservationId,
                            OrderItemId = SeedOrderItemId,
                            InventoryItemId = MainInventoryItemId,
                            WarehouseId = MainWarehouseId,
                            Quantity = 1
                        },
                        new ItemReservation
                        {
                            Id = SeedFreeItemReservationId,
                            OrderItemId = SeedOrderItemId,
                            InventoryItemId = FreeInventoryItemId,
                            WarehouseId = FreeWarehouseId,
                            Quantity = 1
                        }
                    ]
                }
            ],
            Logs =
            [
                new OrderLog
                {
                    Id = SeedOrderLogId,
                    OrderId = SeedOrderId,
                    Action = OrderActions.Place,
                    Description = "Seed order created with main item and free-item reservations",
                    Actor = "Seeder",
                    TimeStamp = DateTimeOffset.UtcNow,
                    StatusBefore = OrderStatus.Initialized,
                    StatusAfter = OrderStatus.Placed
                }
            ]
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}
