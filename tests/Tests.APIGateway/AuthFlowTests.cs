using APIGateway.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.APIGateway;

public class AuthFlowTests : IClassFixture<AuthGatewayFactory>
{
    private readonly AuthGatewayFactory _factory;

    public AuthFlowTests(AuthGatewayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnAccessToken_AndSetRefreshCookie()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new LoginDto("customer@eshop.local", "Customer@123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Any(x => x.Contains("eshop.refresh_token")).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_ShouldRotateToken_WhenRefreshCookieIsValid()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await client.PostAsJsonAsync("/auth/login", new LoginDto("customer@eshop.local", "Customer@123"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var refresh = await client.PostAsync("/auth/refresh", content: null);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await refresh.Content.ReadFromJsonAsync<TokenResponse>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BffCatalogEndpoint_ShouldEnforcePolicy()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var customerToken = await LoginAndGetTokenAsync(client, "customer@eshop.local", "Customer@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var forbidden = await client.GetAsync("/bff/products");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminToken = await LoginAndGetTokenAsync(client, "admin@eshop.local", "Admin@123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var ok = await client.GetAsync("/bff/products");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_ShouldRequireAuthentication()
    {
        var client = _factory.CreateClient();
        var unauthorized = await client.GetAsync("/auth/me");
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var token = await LoginAndGetTokenAsync(client, "customer@eshop.local", "Customer@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var authorized = await client.GetAsync("/auth/me");
        authorized.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReverseProxy_ShouldCreateAndReadGuestOrderHistory_EndToEnd()
    {
        await using var stub = await OrderStubHost.StartAsync();

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var routeConfig = new Dictionary<string, string?>
                {
                    ["ReverseProxy:Routes:order-route:ClusterId"] = "order-cluster",
                    ["ReverseProxy:Routes:order-route:Match:Path"] = "/api/order/{**catch-all}",
                    ["ReverseProxy:Routes:order-route:Transforms:0:PathRemovePrefix"] = "/api/order",
                    ["ReverseProxy:Clusters:order-cluster:Destinations:order-api:Address"] = stub.BaseAddress
                };
                config.AddInMemoryCollection(routeConfig);
            });
        });

        var client = factory.CreateClient();
        var guestId = Guid.CreateVersion7();
        var createOrderPayload = new
        {
            customerName = "Guest A",
            address = "District 1",
            phoneNumber = "0900000000",
            items = new[] { new { productId = Guid.CreateVersion7(), variantId = (Guid?)null, quantity = 1 } }
        };

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/order/orders")
        {
            Content = JsonContent.Create(createOrderPayload)
        };
        createRequest.Headers.Add("X-Guest-Id", guestId.ToString());

        var createResponse = await client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var historyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/order/orders/history");
        historyRequest.Headers.Add("X-Guest-Id", guestId.ToString());
        var historyResponse = await client.SendAsync(historyRequest);

        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyContent = await historyResponse.Content.ReadAsStringAsync();
        historyContent.Should().Contain(guestId.ToString());
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginDto(email, password));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return payload!.AccessToken;
    }

    private sealed record LoginDto(string Email, string Password);
}

public class AuthGatewayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"auth-test-db-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:TestDbName"] = _dbName
            });
        });
    }
}

internal sealed class OrderStubHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    public string BaseAddress { get; private set; } = string.Empty;

    private OrderStubHost(WebApplication app)
    {
        _app = app;
    }

    public static async Task<OrderStubHost> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        var ordersByGuest = new Dictionary<Guid, List<object>>();

        app.MapPost("/orders", async (HttpContext context) =>
        {
            if (!context.Request.Headers.TryGetValue("X-Guest-Id", out var guestIdRaw) ||
                !Guid.TryParse(guestIdRaw.FirstOrDefault(), out var guestId))
            {
                return Results.BadRequest("Missing guest id");
            }

            var body = await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(context.Request.Body)
                       ?? new Dictionary<string, object?>();
            body["guestId"] = guestId;
            if (!ordersByGuest.TryGetValue(guestId, out var orders))
            {
                orders = [];
                ordersByGuest[guestId] = orders;
            }

            orders.Add(body);
            return Results.Accepted("/orders/history");
        });

        app.MapGet("/orders/history", (HttpContext context) =>
        {
            if (!context.Request.Headers.TryGetValue("X-Guest-Id", out var guestIdRaw) ||
                !Guid.TryParse(guestIdRaw.FirstOrDefault(), out var guestId))
            {
                return Results.BadRequest("Missing guest id");
            }

            var orders = ordersByGuest.TryGetValue(guestId, out var found) ? found : [];
            return Results.Ok(orders);
        });

        await app.StartAsync();
        var baseAddress = app.Urls.First();
        return new OrderStubHost(app)
        {
            BaseAddress = baseAddress
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
