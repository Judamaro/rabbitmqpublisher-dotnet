namespace RabbitMQPublisher.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IPublicationRepository"/>.
/// </summary>
/// <param name="dbContext">The database context.</param>
internal sealed class PublicationRepository(AppDbContext dbContext) : IPublicationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Publication>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Publications
            .AsNoTracking()
            .OrderBy(publication => publication.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Publication?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Publications.FirstOrDefaultAsync(publication => publication.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Publication?> GetByRoutingKeyAsync(string routingKey, CancellationToken cancellationToken = default) =>
        dbContext.Publications
            .AsNoTracking()
            .FirstOrDefaultAsync(publication => publication.RoutingKey == routingKey, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RoutingKeyExistsAsync(string routingKey, int? excludedId = null, CancellationToken cancellationToken = default) =>
        dbContext.Publications.AnyAsync(
            publication => publication.RoutingKey == routingKey && (excludedId == null || publication.Id != excludedId),
            cancellationToken);

    /// <inheritdoc />
    public void Add(Publication publication) => dbContext.Publications.Add(publication);

    /// <inheritdoc />
    public void Remove(Publication publication) => dbContext.Publications.Remove(publication);
}
