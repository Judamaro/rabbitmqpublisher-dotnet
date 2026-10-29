namespace RabbitMQPublisher.Infrastructure.Health;

/// <summary>
/// Reports whether the shared RabbitMQ connection is open, opening it when it is not.
/// </summary>
/// <param name="connectionProvider">Provider of the shared connection.</param>
internal sealed class RabbitMQHealthCheck(IRabbitMQConnectionProvider connectionProvider) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            IConnection connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            return connection.IsOpen ? HealthCheckResult.Healthy("The RabbitMQ connection is open.") : new HealthCheckResult(context.Registration.FailureStatus, "The RabbitMQ connection is closed.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ is unreachable.", exception);
        }
    }
}
