namespace RabbitMQPublisher.Domain.Entities;

/// <summary>
/// An error reported for a <see cref="Message"/>, typically by the consumer that failed to process it.
/// </summary>
public sealed class Error
{
    /// <summary>Required by Entity Framework Core.</summary>
    private Error() { }

    /// <summary>Gets the identifier of the error.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the identifier of the message the error belongs to.</summary>
    public int MessageId { get; private set; }

    /// <summary>Gets the error text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Gets the UTC date when the error was registered.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the message the error belongs to.</summary>
    public Message? Message { get; private set; }

    /// <summary>
    /// Creates a new error for a message.
    /// </summary>
    /// <param name="messageId">Identifier of the message the error belongs to.</param>
    /// <param name="text">Error text.</param>
    /// <param name="createdAt">UTC creation date.</param>
    /// <returns>The created error.</returns>
    public static Error Create(int messageId, string text, DateTime createdAt)
    => new() { MessageId = messageId, Text = text, CreatedAt = createdAt };
}
