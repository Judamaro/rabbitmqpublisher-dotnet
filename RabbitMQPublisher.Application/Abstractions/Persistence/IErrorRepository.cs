namespace RabbitMQPublisher.Application.Abstractions.Persistence;

/// <summary>
/// Repository that provides access to <see cref="Error"/> entities.
/// </summary>
public interface IErrorRepository
{
    /// <summary>Adds a new error. Changes are persisted through <see cref="IUnitOfWork"/>.</summary>
    /// <param name="error">The error to add.</param>
    void Add(Error error);
}
