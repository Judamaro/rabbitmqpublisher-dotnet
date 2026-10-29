namespace RabbitMQPublisher.Application.Publications;

/// <summary>
/// Default implementation of <see cref="IPublicationService"/>.
/// </summary>
/// <param name="publicationRepository">Repository of publications.</param>
/// <param name="unitOfWork">Unit of Work that persists the changes.</param>
/// <param name="messageBroker">Broker where the topology is declared.</param>
/// <param name="validator">Validator of publication requests.</param>
/// <param name="logger">Logger.</param>
internal sealed class PublicationService(
    IPublicationRepository publicationRepository,
    IUnitOfWork unitOfWork,
    IMessageBroker messageBroker,
    IValidator<PublicationRequest> validator,
    ILogger<PublicationService> logger) : IPublicationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicationResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Publication> publications = await publicationRepository.GetAllAsync(cancellationToken);
        return publications.Select(PublicationResponse.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<Result<PublicationResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var publication = await publicationRepository.GetByIdAsync(id, cancellationToken);
        return publication is null
            ? PublicationErrors.NotFound(id)
            : PublicationResponse.FromEntity(publication);
    }

    /// <inheritdoc />
    public async Task<Result<PublicationResponse>> CreateAsync(PublicationRequest request, CancellationToken cancellationToken = default)
    {
        Result validation = await ValidateAsync(request, excludedId: null, cancellationToken);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        Result topology = await DeclareTopologyAsync(request, cancellationToken);
        if (topology.IsFailure)
        {
            return topology.Error;
        }

        Publication publication = Publication.Create(request.Queue, request.RoutingKey, request.Exchange);
        publicationRepository.Add(publication);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PublicationResponse.FromEntity(publication);
    }

    /// <inheritdoc />
    public async Task<Result<PublicationResponse>> UpdateAsync(int id, PublicationRequest request, CancellationToken cancellationToken = default)
    {
        var publication = await publicationRepository.GetByIdAsync(id, cancellationToken);
        if (publication is null)
        {
            return PublicationErrors.NotFound(id);
        }

        Result validation = await ValidateAsync(request, excludedId: id, cancellationToken);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        Result topology = await DeclareTopologyAsync(request, cancellationToken);
        if (topology.IsFailure)
        {
            return topology.Error;
        }

        publication.Update(request.Queue, request.RoutingKey, request.Exchange);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PublicationResponse.FromEntity(publication);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var publication = await publicationRepository.GetByIdAsync(id, cancellationToken);
        if (publication is null)
        {
            return PublicationErrors.NotFound(id);
        }

        publicationRepository.Remove(publication);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Validates the request shape and the uniqueness of its routing key.
    /// </summary>
    private async Task<Result> ValidateAsync(PublicationRequest request, int? excludedId, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResultError();
        }

        if (await publicationRepository.RoutingKeyExistsAsync(request.RoutingKey, excludedId, cancellationToken))
        {
            return PublicationErrors.RoutingKeyAlreadyExists(request.RoutingKey);
        }

        return Result.Success();
    }

    /// <summary>
    /// Declares the exchange, queue and binding of the publication in the broker.
    /// </summary>
    private async Task<Result> DeclareTopologyAsync(PublicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await messageBroker.DeclareTopologyAsync(request.Exchange, request.Queue, request.RoutingKey, cancellationToken);
            return Result.Success();
        }
        catch (MessageBrokerException exception)
        {
            logger.LogError(exception, "Topology for routing key {RoutingKey} could not be declared", request.RoutingKey);
            return PublicationErrors.TopologyDeclarationFailed(request.RoutingKey);
        }
    }
}
