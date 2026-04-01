using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests.Common;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:3.13-management")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}

public sealed class CompositeIntegrationFixture : IAsyncLifetime
{
    public PostgreSqlFixture PostgreSql { get; } = new();
    public RabbitMqFixture RabbitMq { get; } = new();

    public string PostgreSqlConnectionString => PostgreSql.ConnectionString;
    public string RabbitMqConnectionString => RabbitMq.ConnectionString;

    public async Task InitializeAsync()
    {
        await PostgreSql.InitializeAsync();
        await RabbitMq.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await RabbitMq.DisposeAsync();
        await PostgreSql.DisposeAsync();
    }
}

