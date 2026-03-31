using Contracts.Common;
using System.Security.Claims;

namespace Inventory.Security;

public static class InventoryAuthorizationHelpers
{
    public static bool HasPermission(ClaimsPrincipal user, string permission)
        => user.Claims.Any(x => x.Type == AuthClaimTypes.Permission && x.Value == permission);

    public static bool CanAccessWarehouse(ClaimsPrincipal user, Guid warehouseId)
        => user.Claims.Any(x => x.Type == AuthClaimTypes.WarehouseId && x.Value == warehouseId.ToString());
}
