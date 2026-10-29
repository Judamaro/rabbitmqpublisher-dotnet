namespace RabbitMQPublisher.Domain.Common;

/// <summary>
/// Describes an expected failure of an operation.
/// </summary>
/// <param name="Code">Stable, machine-readable error code (for example <c>Message.NotFound</c>).</param>
/// <param name="Description">Human-readable description of the error.</param>
/// <param name="Type">Category of the error.</param>
/// <param name="Metadata">Optional additional data that describes the error.</param>
public sealed record ResultError(string Code, string Description, ErrorType Type, IReadOnlyDictionary<string, object?>? Metadata = null)
{
    /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="description">Error description.</param>
    /// <param name="metadata">Optional additional data.</param>
    /// <returns>The created error.</returns>
    public static ResultError Validation(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, description, ErrorType.Validation, metadata);

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="description">Error description.</param>
    /// <returns>The created error.</returns>
    public static ResultError NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="description">Error description.</param>
    /// <returns>The created error.</returns>
    public static ResultError Conflict(string code, string description)
        => new(code, description, ErrorType.Conflict);

    /// <summary>Creates a <see cref="ErrorType.Unavailable"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="description">Error description.</param>
    /// <param name="metadata">Optional additional data.</param>
    /// <returns>The created error.</returns>
    public static ResultError Unavailable(string code, string description, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, description, ErrorType.Unavailable, metadata);
}
