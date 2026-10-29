namespace RabbitMQPublisher.Application.Abstractions.Messaging;

/// <summary>
/// Broker-agnostic description of a message to publish.
/// </summary>
/// <param name="Exchange">Exchange the message is published to.</param>
/// <param name="RoutingKey">Routing key used to route the message to its queue.</param>
/// <param name="MessageId">Identifier of the stored message, sent in the message body and as the message id property.</param>
/// <param name="Type">Message type, sent in the message body and as the type property.</param>
/// <param name="Payload">Serialized JSON payload, sent in the message body.</param>
public sealed record BrokerMessage(string Exchange, string RoutingKey, int MessageId, string Type, string Payload);
