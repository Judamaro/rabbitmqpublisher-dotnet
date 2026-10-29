namespace RabbitMQPublisher.Test.Common;

/// <summary>
/// Time provider that always returns the same instant.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider() : this(DefaultNow) { }

    public override DateTimeOffset GetUtcNow() => now;
}
