using Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Users.Domain;

namespace APIGateway.Auth;

public static class AuthEndpoints
{
    private const string RefreshCookieName = "eshop.refresh_token";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<Account> userManager,
        AuthDbContext dbContext)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Results.Conflict("Email is already used.");
        }

        var normalizedRole = string.IsNullOrWhiteSpace(request.Role)
            ? AuthRoles.Customer
            : request.Role.Trim();

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true
        };

        if (request.ProfileType.Equals("customer", StringComparison.OrdinalIgnoreCase))
        {
            var customerId = Guid.CreateVersion7();
            account.CustomerId = customerId;
            dbContext.Customers.Add(new Customer
            {
                Id = customerId,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                Email = request.Email,
                Address = request.Address ?? new Address(),
                Loyalty = 0
            });
            normalizedRole = AuthRoles.Customer;
        }
        else
        {
            var staffId = Guid.CreateVersion7();
            account.StaffId = staffId;
            dbContext.Staffs.Add(new Staff
            {
                Id = staffId,
                FullName = request.FullName
            });
        }

        var createResult = await userManager.CreateAsync(account, request.Password);
        if (!createResult.Succeeded)
        {
            return Results.BadRequest(createResult.Errors);
        }

        var roleAddResult = await userManager.AddToRoleAsync(account, normalizedRole);
        if (!roleAddResult.Succeeded)
        {
            return Results.BadRequest(roleAddResult.Errors);
        }

        if (request.WarehouseIds is { Count: > 0 })
        {
            foreach (var warehouseId in request.WarehouseIds)
            {
                await userManager.AddClaimAsync(account, new Claim(AuthClaimTypes.WarehouseId, warehouseId.ToString()));
            }
        }

        await dbContext.SaveChangesAsync();
        return Results.Created($"/auth/users/{account.Id}", new { account.Id, account.Email });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<Account> userManager,
        ITokenService tokenService,
        HttpContext httpContext,
        IHostEnvironment environment,
        IOptions<JwtOptions> jwtOptions)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Results.Unauthorized();
        }

        var tokens = await tokenService.IssueTokensAsync(user, httpContext.RequestAborted);
        SetRefreshCookie(httpContext, tokens.RefreshToken, environment, jwtOptions.Value);

        return Results.Ok(new TokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        ITokenService tokenService,
        IHostEnvironment environment,
        IOptions<JwtOptions> jwtOptions)
    {
        if (!httpContext.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            return Results.Unauthorized();
        }

        var tokens = await tokenService.RotateRefreshTokenAsync(refreshToken, httpContext.RequestAborted);
        if (tokens is null)
        {
            return Results.Unauthorized();
        }

        SetRefreshCookie(httpContext, tokens.RefreshToken, environment, jwtOptions.Value);
        return Results.Ok(new TokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext, ITokenService tokenService)
    {
        if (httpContext.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            await tokenService.RevokeRefreshTokenAsync(refreshToken, httpContext.RequestAborted);
        }

        httpContext.Response.Cookies.Delete(RefreshCookieName);
        return Results.Ok();
    }

    [Authorize]
    private static IResult MeAsync(ClaimsPrincipal user)
    {
        var claims = user.Claims.Select(x => new { x.Type, x.Value });
        return Results.Ok(claims);
    }

    private static void SetRefreshCookie(HttpContext httpContext, string refreshToken, IHostEnvironment environment, JwtOptions options)
    {
        httpContext.Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays),
            Path = "/auth/refresh"
        });
    }
}

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string ProfileType,
    string? Role,
    string? PhoneNumber,
    Address? Address,
    List<Guid>? WarehouseIds);

public sealed record LoginRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
