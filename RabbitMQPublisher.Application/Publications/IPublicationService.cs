namespace RabbitMQPublisher.Application.Publications;

/// <summary>
/// Use cases to manage the publications (routing configuration) used to publish messages.
/// </summary>
public interface IPublicationService
{
    /// <summary>Gets all publications.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The publications.</returns>
    Task<IReadOnlyList<PublicationResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a publication by its identifier.</summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The publication, or a not found error.</returns>
    Task<Result<PublicationResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a publication and declares its exchange, queue and binding in the broker.
    /// </summary>
    /// <param name="request">The publication to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created publication, or the error that prevented its creation.</returns>
    Task<Result<PublicationResponse>> CreateAsync(PublicationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a publication and declares its new exchange, queue and binding in the broker.
    /// Previously declared broker objects are not removed.
    /// </summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="request">The new publication values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated publication, or the error that prevented its update.</returns>
    Task<Result<PublicationResponse>> UpdateAsync(int id, PublicationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a publication. Broker objects are not removed.
    /// </summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A successful result, or a not found error.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
