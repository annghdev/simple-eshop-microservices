using FluentAssertions;
using System.Net;
using Tests.Common;

namespace Tests.E2E.Aspire;

[Collection("aspire-e2e")]
public class AspireSmokeE2ETests
{
    private readonly AspireE2ECollectionFixture _fixture;

    public AspireSmokeE2ETests(AspireE2ECollectionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", TestCategories.E2EAspire)]
    public async Task AppHost_ShouldStart_ApiGatewayHealthEndpointShouldBeHealthy()
    {
        var client = _fixture.CreateClient("apigateway");
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", TestCategories.E2EAspire)]
    public Task AppHost_ShouldExposeExpectedResources()
    {
        var expectedResources = new[]
        {
            "catalog",
            "inventory",
            "order",
            "payment",
            "shipping",
            "apigateway"
        };

        var names = _fixture.ResourceNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        names.Should().Contain(expectedResources);
        names.Should().Contain("rabbitmq");
        names.Should().Contain("postgres");
        return Task.CompletedTask;
    }

}

