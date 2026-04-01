using FluentAssertions;
using Inventory.Domain;
using Inventory.Features.InventoryItems;
using Inventory.IntegrationEvents;
using Inventory.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Order.IntegrationEvents;
using Tests.Common;

namespace Tests.Inventory;

public class InventoryReservationSagaIntegrationTests
{
    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public void Start_ShouldCreateReservationCommands_AndTimeout()
    {
        var inventoryItemA = Guid.CreateVersion7();
        var inventoryItemB = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var placed = new OrderPlaced(
            orderId,
            Amount: 100_000,
            PaymentMethod: "Online",
            Items:
            [
                new ItemReservation(inventoryItemA, 1),
                new ItemReservation(inventoryItemB, 2)
            ]);

        var (saga, messages) = ReservationSaga.Start(placed, NullLogger<ReservationSaga>.Instance);

        saga.Id.Should().Be(orderId);
        saga.TotalItems.Should().Be(2);
        saga.Quantities[inventoryItemA].Should().Be(1);
        saga.Quantities[inventoryItemB].Should().Be(2);
        messages.Should().ContainSingle(x => x is InititionTimeout);
        messages.Count(x => x is StartReservationCommand).Should().Be(2);
    }

    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public void HandleReservationFailed_ShouldEmitRollbackCommands_AndInventoryReservationFailed()
    {
        var orderId = Guid.CreateVersion7();
        var itemA = Guid.CreateVersion7();
        var itemB = Guid.CreateVersion7();
        var saga = new ReservationSaga
        {
            Id = orderId,
            TotalItems = 2,
            Quantities =
            {
                [itemA] = 2,
                [itemB] = 1
            },
            Reserved = [itemA, itemB]
        };

        var events = saga.Handle(new ReservationFailed(itemA, 2, orderId), NullLogger<ReservationSaga>.Instance).ToList();

        events.Count(x => x is ReleaseReservationCommand).Should().Be(2);
        events.OfType<InventoryReservationFailed>().Should().ContainSingle(x => x.OrderId == orderId);
        saga.Failed.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public void HandleReservationSucceeded_WhenSagaAlreadyFailed_ShouldCompensateImmediately()
    {
        var orderId = Guid.CreateVersion7();
        var inventoryItem = Guid.CreateVersion7();
        var saga = new ReservationSaga
        {
            Id = orderId,
            Failed = true
        };

        var events = saga.Handle(new ReservationSucceeded(inventoryItem, 3, orderId), NullLogger<ReservationSaga>.Instance).ToList();

        events.OfType<ReleaseReservationCommand>().Should().ContainSingle(x =>
            x.Id == inventoryItem &&
            x.Quantity == 3 &&
            x.OrderId == orderId);
    }
}

