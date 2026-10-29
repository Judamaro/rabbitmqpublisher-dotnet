namespace RabbitMQPublisher.Application.Abstractions.Persistence;

/// <summary>
/// Repository that provides access to <see cref="Message"/> entities.
/// </summary>
public interface IMessageRepository
{
    /// <summary>Gets a message by its identifier.</summary>
    /// <param name="id">Identifier of the message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The message, or <see langword="null"/> when it does not exist.</returns>
    Task<Message?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a message exists.</summary>
    /// <param name="id">Identifier of the message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the message exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new message. Changes are persisted through <see cref="IUnitOfWork"/>.</summary>
    /// <param name="message">The message to add.</param>
    void Add(Message message);
}
