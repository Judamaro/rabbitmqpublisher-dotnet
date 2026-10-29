namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// Adapter that implements <see cref="IMessageBroker"/> with the RabbitMQ .NET client.
/// Each message is published as a JSON <see cref="MessageEnvelope"/> (id, payload and type), persistent
/// and with publisher confirmations, so a completed <see cref="PublishAsync"/> call means the broker
/// accepted and routed the message.
/// </summary>
/// <param name="connectionProvider">Provider of the shared connection.</param>
internal sealed class RabbitMQMessageBroker(IRabbitMQConnectionProvider connectionProvider) : IMessageBroker
{
    private const string JsonContentType = "application/json";

    private static readonly CreateChannelOptions PublisherChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    // The body is not embedded in HTML, so quotes inside the payload are escaped as \" instead of \u0022.
    private static readonly MessageEnvelopeJsonContext EnvelopeJsonContext = new(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

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

                await channel.BasicPublishAsync(message.Exchange, message.RoutingKey, mandatory: true, properties, SerializeBody(message), cancellationToken);
            },
            $"Message '{message.MessageId}' could not be published to exchange '{message.Exchange}'.",
            cancellationToken);

    /// <summary>
    /// Serializes the body published for a message: a <see cref="MessageEnvelope"/> with its database id,
    /// its payload (as a JSON string) and its type.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <returns>The UTF-8 encoded JSON body.</returns>
    internal static byte[] SerializeBody(BrokerMessage message)
        => JsonSerializer.SerializeToUtf8Bytes(new MessageEnvelope(message.MessageId, message.Payload, message.Type), EnvelopeJsonContext.MessageEnvelope);

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
