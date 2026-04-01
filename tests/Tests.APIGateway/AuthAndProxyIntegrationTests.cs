using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using Tests.Common;

namespace Tests.APIGateway;

public class AuthAndProxyIntegrationTests : IClassFixture<AuthGatewayFactory>
{
    private readonly AuthGatewayFactory _factory;

    public AuthAndProxyIntegrationTests(AuthGatewayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public async Task Login_ShouldRejectInvalidPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "customer@eshop.local", password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", TestCategories.Integration)]
    public async Task ReverseProxy_ShouldRejectGuestHistory_WhenGuestHeaderMissing()
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
        var response = await client.GetAsync("/api/order/orders/history");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

