namespace RabbitMQPublisher.Application.Errors;

/// <summary>
/// Default implementation of <see cref="IErrorService"/>.
/// </summary>
/// <param name="errorRepository">Repository of errors.</param>
/// <param name="messageRepository">Repository of messages.</param>
/// <param name="unitOfWork">Unit of Work that persists the changes.</param>
/// <param name="validator">Validator of add requests.</param>
/// <param name="timeProvider">Source of the current time.</param>
internal sealed class ErrorService(IErrorRepository errorRepository, IMessageRepository messageRepository, IUnitOfWork unitOfWork, IValidator<AddErrorRequest> validator, TimeProvider timeProvider) : IErrorService
{
    /// <inheritdoc />
    public async Task<Result<ErrorResponse>> AddAsync(AddErrorRequest request, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToResultError();

        if (!await messageRepository.ExistsAsync(request.MessageId, cancellationToken))
            return MessageErrors.NotFound(request.MessageId);

        Error error = Error.Create(request.MessageId, request.Text, timeProvider.GetUtcNow().UtcDateTime);
        errorRepository.Add(error);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ErrorResponse.FromEntity(error);
    }
}
