namespace RabbitMQPublisher.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Error"/> to the <c>Error</c> table.
/// </summary>
internal sealed class ErrorConfiguration : IEntityTypeConfiguration<Error>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Error> builder)
    {
        builder.ToTable("Error");

        builder.HasKey(error => error.Id);
        builder.Property(error => error.Id).UseIdentityAlwaysColumn();

        builder.Property(error => error.MessageId).IsRequired();
        builder.Property(error => error.Text).IsRequired();
        builder.Property(error => error.CreatedAt).IsRequired();
    }
}
