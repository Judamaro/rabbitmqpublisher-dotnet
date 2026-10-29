namespace RabbitMQPublisher.Application.Abstractions.Persistence;

/// <summary>
/// Repository that provides access to <see cref="Publication"/> entities.
/// </summary>
public interface IPublicationRepository
{
    /// <summary>Gets all publications ordered by identifier, without change tracking.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The publications.</returns>
    Task<IReadOnlyList<Publication>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a publication by its identifier.</summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The publication, or <see langword="null"/> when it does not exist.</returns>
    Task<Publication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Gets a publication by its routing key.</summary>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The publication, or <see langword="null"/> when no publication uses the routing key.</returns>
    Task<Publication?> GetByRoutingKeyAsync(string routingKey, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a publication other than <paramref name="excludedId"/> uses the routing key.</summary>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="excludedId">Identifier of a publication to ignore, used when updating.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the routing key is in use; otherwise <see langword="false"/>.</returns>
    Task<bool> RoutingKeyExistsAsync(string routingKey, int? excludedId = null, CancellationToken cancellationToken = default);

    /// <summary>Adds a new publication. Changes are persisted through <see cref="IUnitOfWork"/>.</summary>
    /// <param name="publication">The publication to add.</param>
    void Add(Publication publication);

    /// <summary>Removes a publication. Changes are persisted through <see cref="IUnitOfWork"/>.</summary>
    /// <param name="publication">The publication to remove.</param>
    void Remove(Publication publication);
}
