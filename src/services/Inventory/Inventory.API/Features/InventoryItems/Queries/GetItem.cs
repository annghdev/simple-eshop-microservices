using Inventory.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Wolverine.Http.Marten;

namespace Inventory.Features.InventoryItems;

public static class GetItemEndpoint
{
    [WolverineGet("inventory/items/{id}")]
    [Authorize(Policy = "CanReadInventory")]
    public static IResult GetById([Document] InventoryItem item, ClaimsPrincipal user)
    {
        if (!InventoryAuthorizationHelpers.CanAccessWarehouse(user, item.WarehouseId))
        {
            return Results.Forbid();
        }

        return Results.Ok(item);
    }
}
