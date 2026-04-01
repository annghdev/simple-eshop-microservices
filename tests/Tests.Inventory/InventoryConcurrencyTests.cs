using FluentAssertions;
using Inventory.Domain;
using Marten;
using Marten.Events.Projections;
using Tests.Common;

namespace Tests.Inventory;

[Collection("integration-containers")]
public class InventoryConcurrencyTests
{
    private readonly CompositeIntegrationFixture _fixture;

    public InventoryConcurrencyTests(CompositeIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", TestCategories.Concurrency)]
    public async Task ParallelReservations_OnSameInventoryItem_ShouldNotOversell()
    {
        var streamId = Guid.CreateVersion7();
        const int initialAvailable = 10;
        const int concurrentRequests = 50;
        var schema = $"inv_concurrency_{Guid.NewGuid():N}";

        await using var store = DocumentStore.For(opts =>
        {
            opts.Connection(_fixture.PostgreSqlConnectionString);
            opts.DatabaseSchemaName = schema;
            opts.Projections.Snapshot<InventoryItem>(SnapshotLifecycle.Inline);
        });

        await using (var setup = store.LightweightSession())
        {
            setup.Events.StartStream<InventoryItem>(
                streamId,
                new ItemInitialized(streamId, Guid.CreateVersion7(), null, Guid.CreateVersion7()),
                new StockAdjusted(streamId, initialAvailable));
            await setup.SaveChangesAsync();
        }

        var success = 0;
        var failed = 0;

        var tasks = Enumerable.Range(0, concurrentRequests).Select(i => Task.Run(async () =>
        {
            var orderId = Guid.CreateVersion7();

            // Simulate retry policy under stream version contention.
            for (var retry = 0; retry < 8; retry++)
            {
                await using var session = store.LightweightSession();
                var stream = await session.Events.FetchForWriting<InventoryItem>(streamId);
                var item = stream.Aggregate ?? throw new InvalidOperationException("Inventory stream aggregate not found.");

                if (item.Available < 1)
                {
                    Interlocked.Increment(ref failed);
                    return;
                }

                stream.AppendOne(new ReservationSucceeded(streamId, 1, orderId));

                try
                {
                    await session.SaveChangesAsync();
                    Interlocked.Increment(ref success);
                    return;
                }
                catch (Exception ex) when (IsConcurrencyException(ex))
                {
                    await Task.Delay(Random.Shared.Next(5, 20));
                }
            }

            Interlocked.Increment(ref failed);
        }));

        await Task.WhenAll(tasks);

        await using var verify = store.QuerySession();
        var finalState = await verify.Events.AggregateStreamAsync<InventoryItem>(streamId);

        finalState.Should().NotBeNull();
        finalState!.Available.Should().BeGreaterThanOrEqualTo(0);
        finalState.Reserved.Should().Be(success);
        success.Should().BeLessThanOrEqualTo(initialAvailable);
        success.Should().Be(initialAvailable);
        failed.Should().Be(concurrentRequests - success);
    }

    private static bool IsConcurrencyException(Exception ex)
    {
        var name = ex.GetType().Name;
        return name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase)
            || name.Contains("UnexpectedMaxEventId", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("pk_mt_events_stream_and_version", StringComparison.OrdinalIgnoreCase)
            || ex.InnerException?.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) == true;
    }
}

