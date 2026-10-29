namespace RabbitMQPublisher.Application.Messages;

/// <summary>
/// Request to store and publish a new message.
/// </summary>
/// <param name="Payload">Serialized JSON payload of the message.</param>
/// <param name="RoutingKey">Routing key of a configured publication that defines where the payload is published.</param>
/// <param name="Type">Message type, used by consumers to interpret the payload.</param>
public sealed record PublishMessageRequest(string Payload, string RoutingKey, string Type);
