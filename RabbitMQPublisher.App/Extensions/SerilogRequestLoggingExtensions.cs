namespace RabbitMQPublisher.App.Extensions;

/// <summary>
/// Request logging configuration.
/// </summary>
internal static class SerilogRequestLoggingExtensions
{
    /// <summary>
    /// Logs every HTTP request with Serilog. Successful health check requests, sent periodically by
    /// Docker, are logged as <see cref="LogEventLevel.Verbose"/> so they do not flood the logs, and
    /// unhealthy health check responses as <see cref="LogEventLevel.Warning"/>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder, for chaining.</returns>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseSerilogRequestLogging(options => options.GetLevel = GetLevel);

    private static LogEventLevel GetLevel(HttpContext context, double elapsedMilliseconds, Exception? exception)
    {
        bool isHealthCheck = context.Request.Path.StartsWithSegments(HealthCheckEndpointRouteBuilderExtensions.LivenessPath);

        if (exception is not null)
            return LogEventLevel.Error;

        // An unhealthy dependency (503 from /health/ready) is reported by the health checks themselves.
        if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return isHealthCheck ? LogEventLevel.Warning : LogEventLevel.Error;

        return isHealthCheck ? LogEventLevel.Verbose : LogEventLevel.Information;
    }
}
