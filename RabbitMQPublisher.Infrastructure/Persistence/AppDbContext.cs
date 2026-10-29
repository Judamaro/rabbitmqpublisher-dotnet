namespace RabbitMQPublisher.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core context of the PostgreSQL database.
/// </summary>
/// <param name="options">Options of the context.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Gets the messages.</summary>
    public DbSet<Message> Messages => Set<Message>();

    /// <summary>Gets the errors reported for messages.</summary>
    public DbSet<Error> Errors => Set<Error>();

    /// <summary>Gets the publications (routing configuration).</summary>
    public DbSet<Publication> Publications => Set<Publication>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
