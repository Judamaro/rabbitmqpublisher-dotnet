namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// Provides the single, long-lived RabbitMQ connection shared by the application.
/// </summary>
internal interface IRabbitMQConnectionProvider
{
    /// <summary>Gets an open connection, creating it on first use or after it was closed.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An open connection.</returns>
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}
