namespace RabbitMQPublisher.Domain.Errors;

/// <summary>
/// Catalog of the expected errors related to <see cref="Entities.Publication"/>.
/// </summary>
public static class PublicationErrors
{
    /// <summary>The publication does not exist.</summary>
    /// <param name="id">Identifier of the publication.</param>
    /// <returns>The error.</returns>
    public static ResultError NotFound(int id) =>
        ResultError.NotFound("Publication.NotFound", $"The publication with id '{id}' was not found.");

    /// <summary>No publication is configured for the routing key.</summary>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The error.</returns>
    public static ResultError RoutingKeyNotFound(string routingKey) =>
        ResultError.NotFound("Publication.RoutingKeyNotFound", $"No publication is configured for the routing key '{routingKey}'.");

    /// <summary>Another publication already uses the routing key.</summary>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The error.</returns>
    public static ResultError RoutingKeyAlreadyExists(string routingKey) =>
        ResultError.Conflict("Publication.RoutingKeyAlreadyExists", $"A publication with the routing key '{routingKey}' already exists.");

    /// <summary>The broker topology (exchange, queue and binding) could not be declared.</summary>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>The error.</returns>
    public static ResultError TopologyDeclarationFailed(string routingKey) =>
        ResultError.Unavailable("Publication.TopologyDeclarationFailed", $"The broker topology for the routing key '{routingKey}' could not be declared.");
}
