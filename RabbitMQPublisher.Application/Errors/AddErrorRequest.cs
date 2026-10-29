namespace RabbitMQPublisher.Application.Errors;

/// <summary>
/// Request to register an error for a message.
/// </summary>
/// <param name="MessageId">Identifier of the message the error belongs to.</param>
/// <param name="Text">Error text.</param>
public sealed record AddErrorRequest(int MessageId, string Text);
