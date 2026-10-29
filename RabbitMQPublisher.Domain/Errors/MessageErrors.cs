namespace RabbitMQPublisher.Domain.Errors;

/// <summary>
/// Catalog of the expected errors related to <see cref="Entities.Message"/>.
/// </summary>
public static class MessageErrors
{
    /// <summary>The message does not exist.</summary>
    /// <param name="id">Identifier of the message.</param>
    /// <returns>The error.</returns>
    public static ResultError NotFound(int id)
        => ResultError.NotFound("Message.NotFound", $"The message with id '{id}' was not found.");

    /// <summary>The message was already published.</summary>
    /// <param name="id">Identifier of the message.</param>
    /// <returns>The error.</returns>
    public static ResultError AlreadyPublished(int id)
        => ResultError.Conflict("Message.AlreadyPublished", $"The message with id '{id}' is already published.");

    /// <summary>The message was already processed.</summary>
    /// <param name="id">Identifier of the message.</param>
    /// <returns>The error.</returns>
    public static ResultError AlreadyProcessed(int id)
        => ResultError.Conflict("Message.AlreadyProcessed", $"The message with id '{id}' is already processed.");

    /// <summary>The message was stored but the broker could not publish it.</summary>
    /// <param name="id">Identifier of the message.</param>
    /// <returns>The error.</returns>
    public static ResultError PublishFailed(int id)
        => ResultError.Unavailable("Message.PublishFailed", $"The message with id '{id}' was stored but could not be published to the broker. It can be republished later.", new Dictionary<string, object?> { ["messageId"] = id });
}
