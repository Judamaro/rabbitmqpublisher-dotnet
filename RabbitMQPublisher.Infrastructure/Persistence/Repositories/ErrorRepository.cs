namespace RabbitMQPublisher.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IErrorRepository"/>.
/// </summary>
/// <param name="dbContext">The database context.</param>
internal sealed class ErrorRepository(AppDbContext dbContext) : IErrorRepository
{
    /// <inheritdoc />
    public void Add(Error error) => dbContext.Errors.Add(error);
}
