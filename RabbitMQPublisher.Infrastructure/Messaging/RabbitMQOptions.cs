namespace RabbitMQPublisher.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ settings, bound from the <c>RabbitMQOptions</c> configuration section.
/// </summary>
public sealed class RabbitMQOptions
{
    /// <summary>Name of the configuration section.</summary>
    public const string SectionName = "RabbitMQOptions";

    /// <summary>Gets or sets the connection name shown in the RabbitMQ management UI.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the broker host name.</summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the user name.</summary>
    [Required]
    public string User { get; set; } = string.Empty;

    /// <summary>Gets or sets the password.</summary>
    [Required]
    public string Pwd { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default queue name. Part of the shared RabbitMQ settings contract; the publisher
    /// routes each message using its <c>Publication</c>, so this value is not used to publish.
    /// </summary>
    public string Queue { get; set; } = string.Empty;

    /// <summary>Gets or sets the AMQP port.</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    /// <summary>Gets or sets the virtual host.</summary>
    [Required]
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the number of unacknowledged messages a consumer may receive. Part of the shared
    /// RabbitMQ settings contract; prefetch applies only to consumers, so the publisher does not use it.
    /// </summary>
    [Range(0, ushort.MaxValue)]
    public int PrefetchCount { get; set; } = 10;

    /// <summary>Gets or sets the heartbeat timeout negotiated with the broker, in seconds.</summary>
    [Range(0, int.MaxValue)]
    public int HeartbeatSeconds { get; set; } = 30;

    /// <summary>Gets or sets the delay between automatic connection recovery attempts, in seconds.</summary>
    [Range(1, int.MaxValue)]
    public int NetworkRecoveryIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum delay between attempts to establish the initial connection, in seconds.
    /// Attempts are retried with exponential backoff up to this delay.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxConnectRetryDelaySeconds { get; set; } = 60;
}
