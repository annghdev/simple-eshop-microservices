using Inventory.API.GrpcServices.Callers;
using Inventory.Features.InventoryItems;
using Order.IntegrationEvents;

namespace Inventory.Messaging;

public static class OrderCancelledBeforeConfirmHandler
{
    public static async Task<IEnumerable<ReleaseReservationCommand>> Handle(OrderCancelledBeforeConfirm e, IGetOrderReservationItemsCaller caller)
    {
        var orderReservation = await caller.GetOrderReservationItemsAsync(e.OrderId);
        var releaseCmds = orderReservation.Items
            .Select(i => new ReleaseReservationCommand(Guid.Parse(i.InventoryItemId), i.Quantity, e.OrderId));

        return releaseCmds;
    }
}

public static class OrderCancelledAfterConfirmHandler
{
    public static async Task<IEnumerable<RestockReservationCommand>> Handle(OrderCancelledAfterConfirm e, IGetOrderReservationItemsCaller caller)
    {
        var orderReservation = await caller.GetOrderReservationItemsAsync(e.OrderId);
        var restockCmds = orderReservation.Items
            .Select(i => new RestockReservationCommand(Guid.Parse(i.InventoryItemId), i.Quantity, e.OrderId));

        return restockCmds;
    }
}
