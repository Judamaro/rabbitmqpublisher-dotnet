namespace RabbitMQPublisher.Domain.Common;

/// <summary>
/// Classifies a <see cref="ResultError"/> so that outer layers can translate it
/// into the appropriate transport-level response (for example, an HTTP status code).
/// </summary>
public enum ErrorType
{
    /// <summary>The input supplied by the caller is invalid.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state of the resource.</summary>
    Conflict,

    /// <summary>A required external dependency is temporarily unavailable.</summary>
    Unavailable
}
