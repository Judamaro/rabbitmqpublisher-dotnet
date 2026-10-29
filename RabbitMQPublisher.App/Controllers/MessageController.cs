namespace RabbitMQPublisher.App.Controllers;

/// <summary>
/// Publishes messages to RabbitMQ and tracks their life cycle.
/// </summary>
/// <param name="messageService">Message use cases.</param>
[Route("api/[controller]")]
public sealed class MessageController(IMessageService messageService) : ApiControllerBase
{
    /// <inheritdoc />
    protected override string Module => ResponseModules.MessageService;

    /// <summary>
    /// Stores a message and publishes it to the exchange configured for its routing key.
    /// </summary>
    /// <remarks>
    /// The message is stored before being published. If the broker is unavailable the response is
    /// <c>503</c>, the message remains stored with <c>published = false</c> and <c>result.messageId</c>
    /// holds its id so it can be republished later. A message with the same payload, type and routing key
    /// as an existing one is rejected with <c>409</c> and <c>result.messageId</c> holds the existing id.
    /// </remarks>
    /// <param name="request">The message to publish.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the published <see cref="MessageResponse"/>.</returns>
    /// <response code="201">The message was stored and published.</response>
    /// <response code="400">The request is invalid (for example, the payload is not valid JSON).</response>
    /// <response code="404">No publication is configured for the routing key.</response>
    /// <response code="409">A message with the same payload, type and routing key already exists.</response>
    /// <response code="503">The message was stored but the broker could not publish it.</response>
    [HttpPost("Publish")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Publish(PublishMessageRequest request, CancellationToken cancellationToken)
        => Respond(await messageService.PublishAsync(request, cancellationToken), "Message published successfully.", StatusCodes.Status201Created);

    /// <summary>
    /// Publishes again a stored message that has not been published yet.
    /// </summary>
    /// <param name="id">Identifier of the message.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the published <see cref="MessageResponse"/>.</returns>
    /// <response code="200">The message was published.</response>
    /// <response code="404">The message, or the publication of its routing key, does not exist.</response>
    /// <response code="409">The message is already published.</response>
    /// <response code="503">The broker could not publish the message.</response>
    [HttpPost("{id:int}/Republish")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Republish(int id, CancellationToken cancellationToken)
        => Respond(await messageService.RepublishAsync(id, cancellationToken), "Message republished successfully.");

    /// <summary>
    /// Marks a message as processed, setting <c>processedAt</c> to the current instant.
    /// </summary>
    /// <param name="id">Identifier of the message.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the updated <see cref="MessageResponse"/>.</returns>
    /// <response code="200">The message was marked as processed.</response>
    /// <response code="404">The message does not exist.</response>
    /// <response code="409">The message is already processed.</response>
    [HttpPatch("{id:int}/UpdateAsProcessed")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsProcessed(int id, CancellationToken cancellationToken)
        => Respond(await messageService.UpdateAsProcessedAsync(id, cancellationToken), "Message marked as processed successfully.");
}
