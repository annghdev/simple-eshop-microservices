using Contracts.Enums;
using FluentAssertions;
using Order.Domain;
using Tests.Common;

namespace Tests.Order;

public class OrderDomainInvariantTests
{
    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void AddItem_ShouldRejectDuplicatedProductAndVariant()
    {
        var order = NewOrder();
        var productId = Guid.CreateVersion7();
        var variantId = Guid.CreateVersion7();
        order.AddItem(NewItem(productId, variantId));

        var act = () => order.AddItem(NewItem(productId, variantId));

        act.Should().Throw<ArgumentException>().WithMessage("*duplicated*");
    }

    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void ConfirmManually_ShouldThrow_WhenOrderIsNotPlaced()
    {
        var order = NewOrder();
        order.Status = OrderStatus.Initialized;

        var act = () => order.ConfirmManually("tester");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Placed*");
    }

    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void Cancel_ShouldThrow_WhenOrderAlreadyShipped()
    {
        var order = NewOrder();
        order.Status = OrderStatus.Shipped;

        var act = () => order.Cancel("customer changed mind", "customer");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already shipped*");
    }

    private static global::Order.Domain.Order NewOrder()
    {
        return new global::Order.Domain.Order
        {
            Id = Guid.CreateVersion7(),
            CustomerName = "User",
            Address = "Address",
            PhoneNumber = "0900000000",
            PaymentMethod = PaymentMethod.Online
        };
    }

    private static OrderItem NewItem(Guid productId, Guid? variantId)
    {
        return new OrderItem
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            VariantId = variantId,
            ItemName = "Test item",
            UnitPrice = 100_000,
            TotalQuantity = 1
        };
    }
}

