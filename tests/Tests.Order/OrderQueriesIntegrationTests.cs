using Contracts.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Order;
using Order.Domain;
using Order.Features.Orders;
using Tests.Common;

namespace Tests.Order;

public class OrderQueriesIntegrationTests
{
    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public async Task GetOrdersByCustomer_ShouldReturnOnlyOrdersOfThatCustomer()
    {
        await using var db = BuildDbContext();
        var expectedCustomer = Guid.CreateVersion7();
        await SeedOrdersAsync(db, expectedCustomer, Guid.CreateVersion7());

        var orders = await GetOrdersByCustomerHandler.Handle(new GetOrdersByCustomerQuery(expectedCustomer), db, CancellationToken.None);

        orders.Should().NotBeEmpty();
        orders.Should().OnlyContain(x => x.CustomerId == expectedCustomer);
    }

    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public async Task GetOrdersByGuest_ShouldReturnOnlyOrdersOfThatGuest()
    {
        await using var db = BuildDbContext();
        var expectedGuest = Guid.CreateVersion7();
        await SeedGuestOrdersAsync(db, expectedGuest, Guid.CreateVersion7());

        var orders = await GetOrdersByGuestHandler.Handle(new GetOrdersByGuestQuery(expectedGuest), db, CancellationToken.None);

        orders.Should().NotBeEmpty();
        orders.Should().OnlyContain(x => x.GuestId == expectedGuest);
    }

    private static OrderDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase($"orders-tests-{Guid.NewGuid():N}")
            .Options;

        return new OrderDbContext(options);
    }

    private static async Task SeedOrdersAsync(OrderDbContext db, Guid targetCustomerId, Guid otherCustomerId)
    {
        db.Orders.Add(CreateOrder(customerId: targetCustomerId, guestId: null));
        db.Orders.Add(CreateOrder(customerId: targetCustomerId, guestId: null));
        db.Orders.Add(CreateOrder(customerId: otherCustomerId, guestId: null));
        await db.SaveChangesAsync();
    }

    private static async Task SeedGuestOrdersAsync(OrderDbContext db, Guid targetGuestId, Guid otherGuestId)
    {
        db.Orders.Add(CreateOrder(customerId: null, guestId: targetGuestId));
        db.Orders.Add(CreateOrder(customerId: null, guestId: targetGuestId));
        db.Orders.Add(CreateOrder(customerId: null, guestId: otherGuestId));
        await db.SaveChangesAsync();
    }

    private static global::Order.Domain.Order CreateOrder(Guid? customerId, Guid? guestId)
    {
        var order = new global::Order.Domain.Order
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            GuestId = guestId,
            CustomerName = "Test Customer",
            Address = "Address",
            PhoneNumber = "0900000000",
            PaymentMethod = PaymentMethod.Online,
            Status = OrderStatus.Placed
        };

        order.AddItem(new OrderItem
        {
            Id = Guid.CreateVersion7(),
            ProductId = Guid.CreateVersion7(),
            VariantId = null,
            ItemName = "Product",
            UnitPrice = 100_000,
            TotalQuantity = 1
        });

        return order;
    }
}

