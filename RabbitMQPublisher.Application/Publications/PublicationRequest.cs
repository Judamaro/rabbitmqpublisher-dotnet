namespace RabbitMQPublisher.Application.Publications;

/// <summary>
/// Request to create or update a publication.
/// </summary>
/// <param name="Queue">Name of the queue bound to the exchange.</param>
/// <param name="RoutingKey">Unique routing key that binds the queue to the exchange.</param>
/// <param name="Exchange">Name of the exchange messages are published to.</param>
public sealed record PublicationRequest(string Queue, string RoutingKey, string Exchange);
