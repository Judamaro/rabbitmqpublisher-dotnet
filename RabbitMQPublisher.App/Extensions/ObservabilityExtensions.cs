namespace RabbitMQPublisher.App.Extensions;

/// <summary>
/// Configures OpenTelemetry metrics and exposes them for Prometheus on a dedicated, internal port.
/// </summary>
internal static class ObservabilityExtensions
{
    /// <summary>Path of the Prometheus scraping endpoint.</summary>
    public const string MetricsPath = "/metrics";

    /// <summary>Configuration key of the port that serves <see cref="MetricsPath"/>.</summary>
    public const string MetricsPortKey = "Metrics:Port";

    /// <summary>Default port of the metrics endpoint (the usual port of the OpenTelemetry Prometheus exporter).</summary>
    public const int DefaultMetricsPort = 9464;

    private const string ServiceName = "rabbitmq-publisher";

    /// <summary>
    /// Meters exported to Prometheus: the publisher business metrics plus the metrics already emitted by
    /// ASP.NET Core (HTTP requests, Kestrel, exceptions), the .NET runtime and Npgsql.
    /// </summary>
    private static readonly string[] Meters =
    [
        PublisherMetrics.MeterName,
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Server.Kestrel",
        "Microsoft.AspNetCore.Diagnostics",
        "System.Runtime",
        "Npgsql"
    ];

    /// <summary>Adds OpenTelemetry metrics with the Prometheus exporter.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithMetrics(metrics => metrics
                .AddMeter(Meters)
                .AddPrometheusExporter());

        return services;
    }

    /// <summary>Gets the port that serves the metrics endpoint.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The value of <see cref="MetricsPortKey"/>, or <see cref="DefaultMetricsPort"/>.</returns>
    public static int GetMetricsPort(this IConfiguration configuration)
        => configuration.GetValue(MetricsPortKey, DefaultMetricsPort);

    /// <summary>
    /// Maps <see cref="MetricsPath"/> only on the metrics port, so it is not reachable through the public
    /// API port. The application must also listen on that port (<c>ASPNETCORE_HTTP_PORTS</c> or the launch profile).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same web application, for chaining.</returns>
    public static WebApplication MapMetricsEndpoint(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint(MetricsPath).RequireHost($"*:{app.Configuration.GetMetricsPort()}");
        return app;
    }
}
