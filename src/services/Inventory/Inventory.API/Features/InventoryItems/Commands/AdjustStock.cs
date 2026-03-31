using Contracts.Common;
using Inventory.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Inventory.Features.InventoryItems;

public record AdjustStockCommand(Guid Id, int Quantity);

public static class AdjustStockHandler
{
    [AggregateHandler]
    public static object Handle(AdjustStockCommand cmd, InventoryItem item)
    {
        var evt = item.AdjustStock(cmd.Quantity);
        return evt;
    }
}

public static class AdjustStockEndpoint
{
    [WolverinePost("inventory/items/adjust")]
    [Authorize(Policy = "CanAdjustInventory")]
    public static async Task<IResult> Post(AdjustStockCommand cmd, ClaimsPrincipal user, IQuerySession session, IMessageBus bus)
    {
        var item = await session.LoadAsync<InventoryItem>(cmd.Id);
        if (item is null)
        {
            return Results.NotFound();
        }

        if (!InventoryAuthorizationHelpers.CanAccessWarehouse(user, item.WarehouseId))
        {
            return Results.Forbid();
        }

        await bus.InvokeAsync(cmd);
        return Results.Ok();
    }
}