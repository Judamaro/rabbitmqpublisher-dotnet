namespace RabbitMQPublisher.Application.Messages;

/// <summary>
/// Representation of a stored message.
/// </summary>
/// <param name="Id">Identifier of the message.</param>
/// <param name="Payload">Serialized JSON payload.</param>
/// <param name="Type">Message type.</param>
/// <param name="RoutingKey">Routing key used to publish the message.</param>
/// <param name="Published">Whether the broker confirmed the message.</param>
/// <param name="PublishedAt">UTC creation date while pending, or UTC publication date once published.</param>
/// <param name="Processed">Whether a consumer reported the message as processed.</param>
/// <param name="ProcessedAt">UTC date when the message was processed.</param>
public sealed record MessageResponse(int Id, string Payload, string Type, string RoutingKey, bool Published, DateTime PublishedAt, bool? Processed, DateTime? ProcessedAt)
{
    /// <summary>Maps a <see cref="Message"/> entity into its response.</summary>
    /// <param name="message">The message entity.</param>
    /// <returns>The response.</returns>
    public static MessageResponse FromEntity(Message message)
        => new(message.Id, message.Payload, message.Type, message.RoutingKey, message.Published, message.PublishedAt, message.Processed, message.ProcessedAt);
}
