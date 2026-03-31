using Catalog.IntegrationEvents;
using Inventory.IntegrationEvents;
using Wolverine;
using Wolverine.Http;

namespace Catalog.Messaging;

public static class TestCommunicationEndpoint
{
    [WolverinePost("catalog/test/communication")]
    public static async Task<IResult> Post(IMessageBus bus)
    {
        await bus.PublishAsync(new CatalogEventPublished());

        return Results.Ok();
    }
}
