namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// Establishes the RabbitMQ connection in the background when the application starts, retrying with
/// exponential backoff (capped by <see cref="RabbitMQOptions.MaxConnectRetryDelaySeconds"/>) so the API
/// can start even while the broker is unavailable. Once connected, the client's automatic recovery
/// takes over.
/// </summary>
/// <param name="connectionProvider">Provider of the shared connection.</param>
/// <param name="options">RabbitMQ settings.</param>
/// <param name="logger">Logger.</param>
internal sealed class RabbitMQConnectionInitializer(IRabbitMQConnectionProvider connectionProvider, IOptions<RabbitMQOptions> options, ILogger<RabbitMQConnectionInitializer> logger) : BackgroundService
{
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan maxDelay = TimeSpan.FromSeconds(options.Value.MaxConnectRetryDelaySeconds);
        TimeSpan delay = InitialRetryDelay;
        int attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                await connectionProvider.GetConnectionAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "RabbitMQ connection attempt {Attempt} failed, retrying in {Delay}", attempt, delay);
            }

            await Task.Delay(delay, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, maxDelay.Ticks));
        }
    }
}
