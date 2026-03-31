using Contracts.Common;
using FluentAssertions;
using Inventory.Security;
using System.Security.Claims;

namespace Tests.Inventory;

public class InventoryOwnershipTests
{
    [Fact]
    public void CanAccessWarehouse_ShouldReturnTrue_WhenWarehouseClaimMatches()
    {
        var warehouseId = Guid.CreateVersion7();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.WarehouseId, warehouseId.ToString())
        ], "test"));

        var result = InventoryAuthorizationHelpers.CanAccessWarehouse(principal, warehouseId);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanAccessWarehouse_ShouldReturnFalse_WhenWarehouseClaimDifferent()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.WarehouseId, Guid.CreateVersion7().ToString())
        ], "test"));

        var result = InventoryAuthorizationHelpers.CanAccessWarehouse(principal, Guid.CreateVersion7());

        result.Should().BeFalse();
    }
}
