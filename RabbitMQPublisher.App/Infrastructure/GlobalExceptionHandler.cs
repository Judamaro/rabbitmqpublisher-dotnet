namespace RabbitMQPublisher.App.Infrastructure;

/// <summary>
/// Converts unhandled exceptions into <see cref="PetitionResponse"/> responses, hiding internal details from clients.
/// </summary>
/// <param name="logger">Logger.</param>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int statusCode, string message) = exception switch
        {
            UniqueConstraintViolationException => (StatusCodes.Status409Conflict, "The resource conflicts with an existing one."),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request is invalid."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning(exception, "Request {Method} {Path} failed", httpContext.Request.Method, httpContext.Request.Path);

        PetitionResponse response = PetitionResponse.Fail(ResponseModules.GlobalExceptionHandler, message, statusCode);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
