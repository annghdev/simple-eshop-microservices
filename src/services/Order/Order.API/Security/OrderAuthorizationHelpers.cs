using Contracts.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Order.Security;

public static class OrderAuthorizationHelpers
{
    public static Guid? GetCustomerId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.CustomerId);
        return Guid.TryParse(value, out var customerId) ? customerId : null;
    }

    public static bool HasPermission(ClaimsPrincipal user, string permission)
        => user.Claims.Any(x => x.Type == AuthClaimTypes.Permission && x.Value == permission);

    public static Guid? GetGuestId(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("X-Guest-Id", out var guestIdRaw))
        {
            return null;
        }

        return Guid.TryParse(guestIdRaw.FirstOrDefault(), out var guestId) ? guestId : null;
    }

    public static bool IsAuthenticated(ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true;
}
