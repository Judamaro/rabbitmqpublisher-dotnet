namespace RabbitMQPublisher.Application.Abstractions.Messaging;

/// <summary>
/// Port to the message broker. Implementations adapt a concrete broker client (Adapter pattern)
/// and must wrap any broker failure in a <see cref="MessageBrokerException"/>.
/// </summary>
public interface IMessageBroker
{
    /// <summary>
    /// Publishes a message and waits until the broker confirms it.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the broker confirms the message.</returns>
    /// <exception cref="MessageBrokerException">Thrown when the broker cannot publish or confirm the message.</exception>
    Task PublishAsync(BrokerMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Declares, idempotently, the exchange, the queue and the binding between them for a routing key.
    /// </summary>
    /// <param name="exchange">Exchange name.</param>
    /// <param name="queue">Queue name.</param>
    /// <param name="routingKey">Routing key that binds the queue to the exchange.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the topology is declared.</returns>
    /// <exception cref="MessageBrokerException">Thrown when the topology cannot be declared.</exception>
    Task DeclareTopologyAsync(string exchange, string queue, string routingKey, CancellationToken cancellationToken = default);
}
