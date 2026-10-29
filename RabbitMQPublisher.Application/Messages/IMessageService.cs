namespace RabbitMQPublisher.Application.Messages;

/// <summary>
/// Use cases of the message life cycle: publication, republication and processing.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Stores a new message and publishes it to the exchange configured for its routing key.
    /// The message is stored before publishing, so it can be republished if the broker fails.
    /// </summary>
    /// <param name="request">The message to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The published message, or the error that prevented its publication.</returns>
    Task<Result<MessageResponse>> PublishAsync(PublishMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes again a stored message that has not been published yet.
    /// </summary>
    /// <param name="id">Identifier of the message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The published message, or the error that prevented its publication.</returns>
    Task<Result<MessageResponse>> RepublishAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a stored message as processed at the current instant.
    /// </summary>
    /// <param name="id">Identifier of the message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated message, or the error that prevented the update.</returns>
    Task<Result<MessageResponse>> UpdateAsProcessedAsync(int id, CancellationToken cancellationToken = default);
}
