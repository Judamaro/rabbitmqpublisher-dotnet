namespace RabbitMQPublisher.Infrastructure.Persistence;

/// <summary>
/// <see cref="IUnitOfWork"/> backed by <see cref="AppDbContext"/>.
/// </summary>
/// <param name="dbContext">The database context shared by the repositories of the request.</param>
internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
        {
            throw new UniqueConstraintViolationException($"The changes violate the unique constraint '{postgresException.ConstraintName}'.", exception);
        }
    }
}
