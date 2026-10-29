namespace RabbitMQPublisher.Application.Errors;

/// <summary>
/// Representation of an error registered for a message.
/// </summary>
/// <param name="Id">Identifier of the error.</param>
/// <param name="MessageId">Identifier of the message the error belongs to.</param>
/// <param name="Text">Error text.</param>
/// <param name="CreatedAt">UTC date when the error was registered.</param>
public sealed record ErrorResponse(int Id, int MessageId, string Text, DateTime CreatedAt)
{
    /// <summary>Maps an <see cref="Error"/> entity into its response.</summary>
    /// <param name="error">The error entity.</param>
    /// <returns>The response.</returns>
    public static ErrorResponse FromEntity(Error error)
        => new(error.Id, error.MessageId, error.Text, error.CreatedAt);
}
