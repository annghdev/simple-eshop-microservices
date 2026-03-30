using Inventory.API.GrpcServices.Callers;
using Inventory.Features.InventoryItems;
using Order.IntegrationEvents;

namespace Inventory.Messaging;

public static class OrderConfirmedHandler
{
    public static async Task<IEnumerable<CommitReservationCommand>> Handle(OrderConfirmed e, IGetOrderReservationItemsCaller caller)
    {
        var orderReservation = await caller
            .GetOrderReservationItemsAsync(e.OrderId);

        var commitCmds = orderReservation.Items
            .Select(i => new CommitReservationCommand(Guid.Parse(i.InventoryItemId), i.Quantity, e.OrderId));

        return commitCmds;
    }
}
