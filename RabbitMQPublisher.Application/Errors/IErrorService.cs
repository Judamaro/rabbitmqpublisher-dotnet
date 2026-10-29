namespace RabbitMQPublisher.Application.Errors;

/// <summary>
/// Use cases of the errors reported for messages.
/// </summary>
public interface IErrorService
{
    /// <summary>Registers an error for an existing message.</summary>
    /// <param name="request">The error to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The registered error, or the error that prevented its registration.</returns>
    Task<Result<ErrorResponse>> AddAsync(AddErrorRequest request, CancellationToken cancellationToken = default);
}
