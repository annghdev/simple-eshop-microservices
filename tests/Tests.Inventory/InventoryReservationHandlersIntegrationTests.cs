using FluentAssertions;
using Inventory.Domain;
using Inventory.Features.InventoryItems;
using Tests.Common;

namespace Tests.Inventory;

public class InventoryReservationHandlersIntegrationTests
{
    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public void CommitReservation_ShouldReduceReserved_WhenApplied()
    {
        var orderId = Guid.CreateVersion7();
        var item = new InventoryItem
        {
            Id = Guid.CreateVersion7(),
            Available = 10,
            Reserved = 4
        };

        var evt = CommitReservationHandler.Handle(new CommitReservationCommand(item.Id, 3, orderId), item);
        item.Apply(evt);

        item.Reserved.Should().Be(1);
        item.Available.Should().Be(10);
    }

    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public void ReleaseReservation_ShouldReturnStockToAvailable_WhenApplied()
    {
        var orderId = Guid.CreateVersion7();
        var item = new InventoryItem
        {
            Id = Guid.CreateVersion7(),
            Available = 5,
            Reserved = 3
        };

        var evt = ReleaseStockHandler.Handle(new ReleaseReservationCommand(item.Id, 2, orderId), item);
        item.Apply(evt);

        item.Reserved.Should().Be(1);
        item.Available.Should().Be(7);
    }
}

