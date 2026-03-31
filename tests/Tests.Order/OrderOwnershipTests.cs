using Contracts.Common;
using FluentAssertions;
using Order.Security;
using System.Security.Claims;

namespace Tests.Order;

public class OrderOwnershipTests
{
    [Fact]
    public void GetCustomerId_ShouldReturnClaimValue_WhenPresent()
    {
        var customerId = Guid.CreateVersion7();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.CustomerId, customerId.ToString())
        ], "test"));

        var result = OrderAuthorizationHelpers.GetCustomerId(principal);

        result.Should().Be(customerId);
    }

    [Fact]
    public void HasPermission_ShouldReturnFalse_WhenPermissionMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaimTypes.Permission, PermissionConstants.Order.ReadOwn)
        ], "test"));

        var result = OrderAuthorizationHelpers.HasPermission(principal, PermissionConstants.Order.ReadAll);

        result.Should().BeFalse();
    }
}
