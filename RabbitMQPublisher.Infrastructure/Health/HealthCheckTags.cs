namespace RabbitMQPublisher.Infrastructure.Health;

/// <summary>
/// Tags used to group the health checks exposed by the API.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>Checks of the external dependencies (PostgreSQL and RabbitMQ), reported by the readiness endpoint.</summary>
    public const string Ready = "ready";
}
