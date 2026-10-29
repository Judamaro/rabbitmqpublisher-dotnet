namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// Adapter that implements <see cref="IMessageBroker"/> with the RabbitMQ .NET client.
/// Messages are published as persistent and with publisher confirmations, so a completed
/// <see cref="PublishAsync"/> call means the broker accepted and routed the message.
/// </summary>
/// <param name="connectionProvider">Provider of the shared connection.</param>
internal sealed class RabbitMQMessageBroker(IRabbitMQConnectionProvider connectionProvider) : IMessageBroker
{
    private const string JsonContentType = "application/json";

    private static readonly CreateChannelOptions PublisherChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    /// <inheritdoc />
    public Task PublishAsync(BrokerMessage message, CancellationToken cancellationToken = default)
        => ExecuteAsync(async channel =>
            {
                BasicProperties properties = new()
                {
                    ContentType = JsonContentType,
                    ContentEncoding = Encoding.UTF8.WebName,
                    DeliveryMode = DeliveryModes.Persistent,
                    MessageId = message.MessageId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Type = message.Type,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await channel.BasicPublishAsync(message.Exchange, message.RoutingKey, mandatory: true, properties, Encoding.UTF8.GetBytes(message.Payload), cancellationToken);
            },
            $"Message '{message.MessageId}' could not be published to exchange '{message.Exchange}'.",
            cancellationToken);

    /// <inheritdoc />
    public Task DeclareTopologyAsync(string exchange, string queue, string routingKey, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async channel =>
            {
                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);
                await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
                await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: cancellationToken);
            },
            $"Topology for routing key '{routingKey}' could not be declared.",
            cancellationToken);

    /// <summary>
    /// Runs an operation on a short-lived channel and translates client failures into <see cref="MessageBrokerException"/>.
    /// </summary>
    private async Task ExecuteAsync(Func<IChannel, Task> operation, string failureMessage, CancellationToken cancellationToken)
    {
        try
        {
            IConnection connection = await connectionProvider.GetConnectionAsync(cancellationToken);
            await using IChannel channel = await connection.CreateChannelAsync(PublisherChannelOptions, cancellationToken);
            await operation(channel);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new MessageBrokerException(failureMessage, exception);
        }
    }
}
