namespace RabbitMQPublisher.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Publication"/> to the <c>Publication</c> table.
/// </summary>
internal sealed class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("Publication");

        builder.HasKey(publication => publication.Id);
        builder.Property(publication => publication.Id).UseIdentityAlwaysColumn();

        builder.Property(publication => publication.Queue).HasMaxLength(255).IsRequired();
        builder.Property(publication => publication.RoutingKey).HasMaxLength(255).IsRequired();
        builder.Property(publication => publication.Exchange).HasMaxLength(255).IsRequired();

        builder.HasIndex(publication => publication.RoutingKey).IsUnique();
    }
}
