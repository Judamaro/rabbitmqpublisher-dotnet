namespace RabbitMQPublisher.App.Controllers;

/// <summary>
/// Registers errors reported for messages.
/// </summary>
/// <param name="errorService">Error use cases.</param>
[Route("api/[controller]")]
public sealed class ErrorController(IErrorService errorService) : ApiControllerBase
{
    /// <inheritdoc />
    protected override string Module => ResponseModules.ErrorService;

    /// <summary>
    /// Registers an error for an existing message.
    /// </summary>
    /// <param name="request">The error to register.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the registered <see cref="ErrorResponse"/>.</returns>
    /// <response code="201">The error was registered.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">The message does not exist.</response>
    [HttpPost("Add")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(AddErrorRequest request, CancellationToken cancellationToken)
        => Respond(await errorService.AddAsync(request, cancellationToken), "Error registered successfully.", StatusCodes.Status201Created);
}
