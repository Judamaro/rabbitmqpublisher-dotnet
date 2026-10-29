namespace RabbitMQPublisher.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Message"/> to the <c>Message</c> table.
/// </summary>
internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Message");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).UseIdentityAlwaysColumn();

        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.Type).HasMaxLength(200).IsRequired();
        builder.Property(message => message.PublishedAt).IsRequired();
        builder.Property(message => message.Published).IsRequired();
        builder.Property(message => message.ProcessedAt);
        builder.Property(message => message.Processed);
        builder.Property(message => message.RoutingKey).HasMaxLength(255).IsRequired();

        builder.HasIndex(message => message.RoutingKey);

        builder.HasMany(message => message.Errors)
            .WithOne(error => error.Message)
            .HasForeignKey(error => error.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(message => message.Errors)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
