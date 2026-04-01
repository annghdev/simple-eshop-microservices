using Aspire.Hosting;
using Aspire.Hosting.Testing;
using System.Collections.Concurrent;

namespace Tests.E2E.Aspire;

public sealed class AspireE2ECollectionFixture : IAsyncLifetime
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    private DistributedApplication? _application;
    private string[] _resourceNames = [];

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Aspire_AppHost>(
                ["DcpPublisher:RandomizePorts=false"],
                cancellationToken: cts.Token);
        _resourceNames = builder.Resources.Select(x => x.Name).ToArray();

        _application = await builder.BuildAsync(cts.Token);
        await _application.StartAsync(cts.Token);
        await _application.ResourceNotifications.WaitForResourceHealthyAsync("apigateway", cts.Token);
        await _application.ResourceNotifications.WaitForResourceHealthyAsync("inventory", cts.Token);
        await _application.ResourceNotifications.WaitForResourceHealthyAsync("order", cts.Token);
    }

    public HttpClient CreateClient(string resourceName)
    {
        if (_application is null)
        {
            throw new InvalidOperationException("Aspire application is not started.");
        }

        return _clients.GetOrAdd(resourceName, name => _application.CreateHttpClient(name));
    }

    public IReadOnlyCollection<string> ResourceNames
        => _resourceNames;

    public async Task DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();

        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }
}

[CollectionDefinition("aspire-e2e", DisableParallelization = true)]
public sealed class AspireE2ECollection : ICollectionFixture<AspireE2ECollectionFixture>
{
}

