namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// Thread-safe, lazily created RabbitMQ connection, registered as a singleton.
/// RabbitMQ recommends one long-lived connection per application and short-lived channels.
/// </summary>
/// <param name="options">RabbitMQ connection settings.</param>
/// <param name="logger">Logger.</param>
internal sealed class RabbitMQConnectionProvider(IOptions<RabbitMQOptions> options, ILogger<RabbitMQConnectionProvider> logger) : IRabbitMQConnectionProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    /// <inheritdoc />
    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection is not null)
                await _connection.DisposeAsync();

            RabbitMQOptions settings = options.Value;
            ConnectionFactory factory = new()
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.User,
                Password = settings.Pwd,
                VirtualHost = settings.VirtualHost,
                ClientProvidedName = settings.Name,
                RequestedHeartbeat = TimeSpan.FromSeconds(settings.HeartbeatSeconds),
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(settings.NetworkRecoveryIntervalSeconds)
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", settings.Host, settings.Port);

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
