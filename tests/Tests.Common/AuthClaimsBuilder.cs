using Contracts.Common;
using System.Security.Claims;

namespace Tests.Common;

public sealed class AuthClaimsBuilder
{
    private readonly List<Claim> _claims = [];
    private bool _isAuthenticated = true;
    private string _authenticationType = "test-auth";

    public AuthClaimsBuilder Authenticated(bool isAuthenticated = true)
    {
        _isAuthenticated = isAuthenticated;
        if (!isAuthenticated)
        {
            _authenticationType = string.Empty;
        }

        return this;
    }

    public AuthClaimsBuilder WithCustomer(Guid customerId)
    {
        _claims.Add(new Claim(AuthClaimTypes.CustomerId, customerId.ToString()));
        return this;
    }

    public AuthClaimsBuilder WithWarehouse(Guid warehouseId)
    {
        _claims.Add(new Claim(AuthClaimTypes.WarehouseId, warehouseId.ToString()));
        return this;
    }

    public AuthClaimsBuilder WithPermission(string permission)
    {
        _claims.Add(new Claim(AuthClaimTypes.Permission, permission));
        return this;
    }

    public AuthClaimsBuilder WithName(string name)
    {
        _claims.Add(new Claim(ClaimTypes.Name, name));
        return this;
    }

    public ClaimsPrincipal Build()
    {
        var identity = new ClaimsIdentity(_claims, _isAuthenticated ? _authenticationType : null);
        return new ClaimsPrincipal(identity);
    }
}

