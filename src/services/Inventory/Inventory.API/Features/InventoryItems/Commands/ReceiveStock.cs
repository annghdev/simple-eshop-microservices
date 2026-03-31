using Contracts.Common;
using Inventory.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Inventory.Features.InventoryItems;

public record ReceiveStockCommand(Guid Id, int Quantity);

public static class ReceiveStockHandler
{
    [AggregateHandler]
    public static StockReceived Handle(ReceiveStockCommand cmd, InventoryItem item)
    {
        var evt = item.Receive(cmd.Quantity);
        return evt;
    }
}

public static class ReceiveStockEndpoint
{
    [WolverinePut("inventory/items/{id}/receive")]
    [Authorize(Policy = "CanReceiveInventory")]
    public static async Task<IResult> Put(Guid id, ReceiveStockCommand cmd, ClaimsPrincipal user, IQuerySession session, IMessageBus bus)
    {
        var item = await session.LoadAsync<InventoryItem>(id);
        if (item is null)
        {
            return Results.NotFound();
        }

        if (!InventoryAuthorizationHelpers.CanAccessWarehouse(user, item.WarehouseId))
        {
            return Results.Forbid();
        }

        cmd = cmd with { Id = id };
        await bus.InvokeAsync(cmd);
        return Results.Ok();
    }
}