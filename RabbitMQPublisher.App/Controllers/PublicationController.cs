namespace RabbitMQPublisher.App.Controllers;

/// <summary>
/// Manages the publications: the routing configuration that maps each unique routing key
/// to the RabbitMQ exchange and queue where messages are published.
/// </summary>
/// <param name="publicationService">Publication use cases.</param>
[Route("api/[controller]")]
public sealed class PublicationController(IPublicationService publicationService) : ApiControllerBase
{
    /// <inheritdoc />
    protected override string Module => ResponseModules.PublicationService;

    /// <summary>Gets all publications.</summary>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the list of <see cref="PublicationResponse"/>.</returns>
    /// <response code="200">The publications.</response>
    [HttpGet]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        PetitionResponse.Ok(
            Module,
            "Publications retrieved successfully.",
            await publicationService.GetAllAsync(cancellationToken)).ToActionResult();

    /// <summary>Gets a publication by its identifier.</summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the <see cref="PublicationResponse"/>.</returns>
    /// <response code="200">The publication.</response>
    /// <response code="404">The publication does not exist.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Respond(await publicationService.GetByIdAsync(id, cancellationToken), "Publication retrieved successfully.");

    /// <summary>
    /// Creates a publication and declares its durable direct exchange, durable queue and binding in RabbitMQ.
    /// </summary>
    /// <param name="request">The publication to create.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the created <see cref="PublicationResponse"/>.</returns>
    /// <response code="201">The publication was created.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="409">Another publication already uses the routing key.</response>
    /// <response code="503">The broker topology could not be declared.</response>
    [HttpPost]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(PublicationRequest request, CancellationToken cancellationToken)
    {
        Result<PublicationResponse> result = await publicationService.CreateAsync(request, cancellationToken);
        ObjectResult response = Respond(result, "Publication created successfully.", StatusCodes.Status201Created);

        if (result.IsSuccess)
        {
            Response.Headers.Location = Url.Action(nameof(GetById), new { id = result.Value.Id });
        }

        return response;
    }

    /// <summary>
    /// Updates a publication and declares its new topology in RabbitMQ. Previous broker objects are kept.
    /// </summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="request">The new publication values.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response whose result is the updated <see cref="PublicationResponse"/>.</returns>
    /// <response code="200">The publication was updated.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">The publication does not exist.</response>
    /// <response code="409">Another publication already uses the routing key.</response>
    /// <response code="503">The broker topology could not be declared.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(int id, PublicationRequest request, CancellationToken cancellationToken) =>
        Respond(await publicationService.UpdateAsync(id, request, cancellationToken), "Publication updated successfully.");

    /// <summary>
    /// Deletes a publication. Broker objects are kept.
    /// </summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>A petition response without result.</returns>
    /// <response code="200">The publication was deleted.</response>
    /// <response code="404">The publication does not exist.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<PetitionResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Respond(await publicationService.DeleteAsync(id, cancellationToken), "Publication deleted successfully.");
}
