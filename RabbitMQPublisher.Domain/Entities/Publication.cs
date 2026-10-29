namespace RabbitMQPublisher.Domain.Entities;

/// <summary>
/// Routing configuration that maps a unique routing key to the RabbitMQ exchange and queue
/// where messages with that routing key are published.
/// </summary>
public sealed class Publication
{
    /// <summary>Required by Entity Framework Core.</summary>
    private Publication() { }

    /// <summary>Gets the identifier of the publication.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the name of the queue bound to the exchange.</summary>
    public string Queue { get; private set; } = string.Empty;

    /// <summary>Gets the unique routing key that binds the queue to the exchange.</summary>
    public string RoutingKey { get; private set; } = string.Empty;

    /// <summary>Gets the name of the exchange messages are published to.</summary>
    public string Exchange { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new publication.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    /// <param name="routingKey">Unique routing key.</param>
    /// <param name="exchange">Exchange name.</param>
    /// <returns>The created publication.</returns>
    public static Publication Create(string queue, string routingKey, string exchange)
        => new() { Queue = queue, RoutingKey = routingKey, Exchange = exchange };

    /// <summary>
    /// Updates the routing configuration of the publication.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    /// <param name="routingKey">Unique routing key.</param>
    /// <param name="exchange">Exchange name.</param>
    public void Update(string queue, string routingKey, string exchange)
    {
        Queue = queue;
        RoutingKey = routingKey;
        Exchange = exchange;
    }
}
