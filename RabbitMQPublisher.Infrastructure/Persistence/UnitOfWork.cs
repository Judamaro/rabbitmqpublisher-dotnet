namespace RabbitMQPublisher.Infrastructure.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> backed by <see cref="AppDbContext"/>.
/// </summary>
/// <param name="dbContext">The database context shared by the repositories of the request.</param>
internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
