namespace RabbitMQPublisher.Domain.Entities;

/// <summary>
/// A message sent to RabbitMQ, tracked through its publication and processing life cycle.
/// </summary>
public sealed class Message
{
    private readonly List<Error> _errors = [];

    /// <summary>Required by Entity Framework Core.</summary>
    private Message()
    {
    }

    /// <summary>Gets the identifier of the message.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the serialized JSON payload of the message.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Gets the type of the message, used by consumers to interpret the payload.</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC date of the last publication attempt state change: the creation date while the message
    /// is pending and the date it was confirmed by the broker once published.
    /// </summary>
    public DateTime PublishedAt { get; private set; }

    /// <summary>Gets a value indicating whether the message was confirmed by the broker.</summary>
    public bool Published { get; private set; }

    /// <summary>Gets the UTC date when a consumer reported the message as processed.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>Gets a value indicating whether a consumer reported the message as processed.</summary>
    public bool? Processed { get; private set; }

    /// <summary>Gets the routing key used to publish the message.</summary>
    public string RoutingKey { get; private set; } = string.Empty;

    /// <summary>Gets the errors reported for the message.</summary>
    public IReadOnlyCollection<Error> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Creates a new message pending publication.
    /// </summary>
    /// <param name="payload">Serialized JSON payload.</param>
    /// <param name="type">Message type.</param>
    /// <param name="routingKey">Routing key used to publish the message.</param>
    /// <param name="createdAt">UTC creation date.</param>
    /// <returns>The created message.</returns>
    public static Message Create(string payload, string type, string routingKey, DateTime createdAt) =>
        new()
        {
            Payload = payload,
            Type = type,
            RoutingKey = routingKey,
            PublishedAt = createdAt,
            Published = false
        };

    /// <summary>
    /// Marks the message as published (confirmed by the broker).
    /// </summary>
    /// <param name="publishedAt">UTC date of the publication.</param>
    /// <returns>A failed result when the message was already published; otherwise a successful result.</returns>
    public Result MarkAsPublished(DateTime publishedAt)
    {
        if (Published)
        {
            return MessageErrors.AlreadyPublished(Id);
        }

        Published = true;
        PublishedAt = publishedAt;
        return Result.Success();
    }

    /// <summary>
    /// Marks the message as processed by a consumer.
    /// </summary>
    /// <param name="processedAt">UTC date of the processing.</param>
    /// <returns>A failed result when the message was already processed; otherwise a successful result.</returns>
    public Result MarkAsProcessed(DateTime processedAt)
    {
        if (Processed == true)
        {
            return MessageErrors.AlreadyProcessed(Id);
        }

        Processed = true;
        ProcessedAt = processedAt;
        return Result.Success();
    }
}
