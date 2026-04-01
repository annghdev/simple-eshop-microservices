using FluentAssertions;
using Inventory.Domain;
using Tests.Common;

namespace Tests.Inventory;

public class InventoryDomainInvariantTests
{
    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void Create_ShouldThrow_WhenProductIdIsEmpty()
    {
        var act = () => InventoryItem.Create(Guid.Empty, null, Guid.CreateVersion7());

        act.Should().Throw<ArgumentException>().WithMessage("*product*");
    }

    [Fact]
    [Trait("Category", TestCategories.Unit)]
    public void Transfer_ShouldThrow_WhenQuantityExceedsAvailable()
    {
        var item = new InventoryItem
        {
            Id = Guid.CreateVersion7(),
            Available = 1
        };

        var act = () => item.Tranfer(2, Guid.CreateVersion7());

        act.Should().Throw<InvalidOperationException>().WithMessage("*not enough*");
    }

    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public void ChangeLowStockAlert_ShouldRejectNegativeQuantity()
    {
        var item = new InventoryItem { Id = Guid.CreateVersion7() };

        var act = () => item.ChangeLowStockAlert(-1);

        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }
}

