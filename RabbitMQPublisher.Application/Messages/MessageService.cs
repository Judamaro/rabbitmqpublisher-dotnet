namespace RabbitMQPublisher.Application.Messages;

/// <summary>
/// Default implementation of <see cref="IMessageService"/>.
/// </summary>
/// <param name="messageRepository">Repository of messages.</param>
/// <param name="publicationRepository">Repository of publications.</param>
/// <param name="unitOfWork">Unit of Work that persists the changes.</param>
/// <param name="messageBroker">Broker the messages are published to.</param>
/// <param name="validator">Validator of publish requests.</param>
/// <param name="timeProvider">Source of the current time.</param>
/// <param name="logger">Logger.</param>
internal sealed class MessageService(IMessageRepository messageRepository, IPublicationRepository publicationRepository, IUnitOfWork unitOfWork, IMessageBroker messageBroker, IValidator<PublishMessageRequest> validator,
    TimeProvider timeProvider, ILogger<MessageService> logger) : IMessageService
{
    /// <inheritdoc />
    public async Task<Result<MessageResponse>> PublishAsync(PublishMessageRequest request, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToResultError();

        var publication = await publicationRepository.GetByRoutingKeyAsync(request.RoutingKey, cancellationToken);
        if (publication is null)
            return PublicationErrors.RoutingKeyNotFound(request.RoutingKey);

        int? existingId = await messageRepository.FindIdByContentAsync(request.RoutingKey, request.Type, request.Payload, cancellationToken);
        if (existingId.HasValue)
            return MessageErrors.Duplicate(existingId.Value);

        Message message = Message.Create(request.Payload, request.Type, request.RoutingKey, UtcNow());
        messageRepository.Add(message);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
        {
            // A concurrent request stored the same message after the previous check.
            existingId = await messageRepository.FindIdByContentAsync(request.RoutingKey, request.Type, request.Payload, cancellationToken);
            if (!existingId.HasValue)
                throw;

            logger.LogWarning(exception, "Duplicate message rejected, it already exists with id {MessageId}", existingId.Value);
            return MessageErrors.Duplicate(existingId.Value);
        }

        return await PublishToBrokerAsync(message, publication, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<MessageResponse>> RepublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await messageRepository.GetByIdAsync(id, cancellationToken);
        if (message is null)
            return MessageErrors.NotFound(id);

        if (message.Published)
            return MessageErrors.AlreadyPublished(id);

        var publication = await publicationRepository.GetByRoutingKeyAsync(message.RoutingKey, cancellationToken);
        if (publication is null)
            return PublicationErrors.RoutingKeyNotFound(message.RoutingKey);

        return await PublishToBrokerAsync(message, publication, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<MessageResponse>> UpdateAsProcessedAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await messageRepository.GetByIdAsync(id, cancellationToken);
        if (message is null)
            return MessageErrors.NotFound(id);

        Result result = message.MarkAsProcessed(UtcNow());
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MessageResponse.FromEntity(message);
    }

    /// <summary>
    /// Sends a stored message to the broker and, once confirmed, marks it as published.
    /// </summary>
    private async Task<Result<MessageResponse>> PublishToBrokerAsync(Message message, Publication publication, CancellationToken cancellationToken)
    {
        BrokerMessage brokerMessage = new(publication.Exchange, publication.RoutingKey, message.Id, message.Type, message.Payload);

        try
        {
            await messageBroker.PublishAsync(brokerMessage, cancellationToken);
        }
        catch (MessageBrokerException exception)
        {
            logger.LogError(exception, "Message {MessageId} could not be published to exchange {Exchange}", message.Id, publication.Exchange);
            return MessageErrors.PublishFailed(message.Id);
        }

        Result result = message.MarkAsPublished(UtcNow());
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Message {MessageId} published to exchange {Exchange} with routing key {RoutingKey}", message.Id, publication.Exchange, publication.RoutingKey);

        return MessageResponse.FromEntity(message);
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
