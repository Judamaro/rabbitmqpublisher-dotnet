namespace RabbitMQPublisher.App.Models;

/// <summary>
/// Names reported in <see cref="PetitionResponse.Module"/>.
/// </summary>
public static class ResponseModules
{
    /// <summary>Message use cases (publish, republish and processing).</summary>
    public const string MessageService = "MessageService";

    /// <summary>Error use cases.</summary>
    public const string ErrorService = "ErrorService";

    /// <summary>Publication use cases.</summary>
    public const string PublicationService = "PublicationService";

    /// <summary>Validation of the request format performed before reaching a service.</summary>
    public const string RequestValidation = "RequestValidation";

    /// <summary>Handler of unexpected exceptions.</summary>
    public const string GlobalExceptionHandler = "GlobalExceptionHandler";

    /// <summary>HTTP pipeline (unknown routes, unsupported methods or media types).</summary>
    public const string Api = "Api";
}
