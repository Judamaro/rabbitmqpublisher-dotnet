namespace RabbitMQPublisher.Application.Abstractions.Persistence;

/// <summary>
/// Unit of Work that persists, as a single transaction, the changes tracked by the repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of affected entries.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
