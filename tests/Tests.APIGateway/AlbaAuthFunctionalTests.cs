using Tests.Common;

namespace Tests.APIGateway;

public class AlbaAuthFunctionalTests
{
    [Fact]
    [Trait("Category", TestCategories.Functional)]
    public async Task MeEndpoint_ShouldReturnUnauthorized_WhenNoTokenProvided()
    {
        await using var host = await AlbaHostFactory.CreateAsync<Program>(
            config: new Dictionary<string, string?>
            {
                ["Auth:TestDbName"] = $"auth-test-db-{Guid.NewGuid():N}"
            });

        await host.Scenario(api =>
        {
            api.Get.Url("/auth/me");
            api.StatusCodeShouldBe(401);
        });
    }
}

