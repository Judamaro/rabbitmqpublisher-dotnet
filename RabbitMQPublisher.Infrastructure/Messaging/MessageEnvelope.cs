namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// Body published to RabbitMQ for every message.
/// </summary>
/// <param name="Id">Identifier of the message stored in the database.</param>
/// <param name="Payload">Serialized JSON payload, sent as a string so consumers receive it unchanged.</param>
/// <param name="Type">Message type, used by consumers to interpret the payload.</param>
internal sealed record MessageEnvelope(int Id, string Payload, string Type);
