using Contracts.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace APIGateway.Auth;

public interface ITokenService
{
    Task<AuthTokenResult> IssueTokensAsync(Account user, CancellationToken ct = default);
    Task<AuthTokenResult?> RotateRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default);
}

public sealed record AuthTokenResult(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);

public class TokenService(
    IOptions<JwtOptions> jwtOptions,
    UserManager<Account> userManager,
    RoleManager<Role> roleManager,
    AuthDbContext dbContext) : ITokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public async Task<AuthTokenResult> IssueTokensAsync(Account user, CancellationToken ct = default)
    {
        var accessToken = await BuildAccessTokenAsync(user);
        var refreshTokenRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            AccountId = user.Id,
            TokenHash = HashToken(refreshTokenRaw),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays)
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);

        return new AuthTokenResult(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshTokenRaw);
    }

    public async Task<AuthTokenResult?> RotateRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var hashed = HashToken(rawRefreshToken);
        var existing = await dbContext.RefreshTokens
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.TokenHash == hashed, ct);
        if (existing is null || !existing.IsActive)
        {
            return null;
        }

        var newRawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newHashedRefreshToken = HashToken(newRawRefreshToken);

        existing.RevokedAtUtc = DateTimeOffset.UtcNow;
        existing.ReplacedByTokenHash = newHashedRefreshToken;

        var replacement = new RefreshToken
        {
            AccountId = existing.AccountId,
            TokenHash = newHashedRefreshToken,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays)
        };
        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(ct);

        var accessToken = await BuildAccessTokenAsync(existing.Account);
        return new AuthTokenResult(accessToken.Token, accessToken.ExpiresAtUtc, newRawRefreshToken);
    }

    public async Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var hashed = HashToken(rawRefreshToken);
        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hashed, ct);
        if (existing is null || !existing.IsActive)
        {
            return;
        }

        existing.RevokedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<(string Token, DateTimeOffset ExpiresAtUtc)> BuildAccessTokenAsync(Account user)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:Jwt:SigningKey is missing or too short.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString())
        };

        if (user.CustomerId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.CustomerId, user.CustomerId.Value.ToString()));
        }

        if (user.StaffId.HasValue)
        {
            claims.Add(new Claim(AuthClaimTypes.StaffId, user.StaffId.Value.ToString()));
        }

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            var roleEntity = await roleManager.FindByNameAsync(role);
            if (roleEntity is null)
            {
                continue;
            }

            var roleClaims = await roleManager.GetClaimsAsync(roleEntity);
            claims.AddRange(roleClaims.Where(c => c.Type == AuthClaimTypes.Permission));
        }

        var userClaims = await userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims.Where(c => c.Type is AuthClaimTypes.Permission or AuthClaimTypes.WarehouseId));

        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var descriptor = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(descriptor);
        return (token, expires);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
