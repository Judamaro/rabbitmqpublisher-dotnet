namespace RabbitMQPublisher.Infrastructure.Persistence.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IMessageRepository"/>.
/// </summary>
/// <param name="dbContext">The database context.</param>
internal sealed class MessageRepository(AppDbContext dbContext) : IMessageRepository
{
    /// <inheritdoc />
    public Task<Message?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.Messages.FirstOrDefaultAsync(message => message.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.Messages.AnyAsync(message => message.Id == id, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The payload parameter takes the <c>jsonb</c> mapping of the column, so PostgreSQL compares it as JSON.</remarks>
    public Task<int?> FindIdByContentAsync(string routingKey, string type, string payload, CancellationToken cancellationToken = default)
        => dbContext.Messages
            .AsNoTracking()
            .Where(message => message.RoutingKey == routingKey && message.Type == type && message.Payload == payload)
            .Select(message => (int?)message.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(Message message) => dbContext.Messages.Add(message);
}
