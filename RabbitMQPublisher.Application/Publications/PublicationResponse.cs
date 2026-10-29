namespace RabbitMQPublisher.Application.Publications;

/// <summary>
/// Representation of a publication.
/// </summary>
/// <param name="Id">Identifier of the publication.</param>
/// <param name="Queue">Name of the queue bound to the exchange.</param>
/// <param name="RoutingKey">Unique routing key that binds the queue to the exchange.</param>
/// <param name="Exchange">Name of the exchange messages are published to.</param>
public sealed record PublicationResponse(int Id, string Queue, string RoutingKey, string Exchange)
{
    /// <summary>Maps a <see cref="Publication"/> entity into its response.</summary>
    /// <param name="publication">The publication entity.</param>
    /// <returns>The response.</returns>
    public static PublicationResponse FromEntity(Publication publication) =>
        new(publication.Id, publication.Queue, publication.RoutingKey, publication.Exchange);
}
