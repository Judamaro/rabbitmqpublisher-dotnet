namespace RabbitMQPublisher.Application.Common;

/// <summary>
/// Business metrics of the publisher, emitted through the <see cref="MeterName"/> meter with
/// <see cref="System.Diagnostics.Metrics"/>. The exporter (Prometheus) is configured in the App layer.
/// </summary>
public sealed class PublisherMetrics
{
    /// <summary>Name of the meter that groups the publisher metrics.</summary>
    public const string MeterName = "RabbitMQPublisher";

    /// <summary>Operation tag value of a first publication.</summary>
    public const string PublishOperation = "publish";

    /// <summary>Operation tag value of a republication.</summary>
    public const string RepublishOperation = "republish";

    // Bucket boundaries in seconds, the same ones ASP.NET Core uses for http.server.request.duration.
    // Without them OpenTelemetry uses its default boundaries (0, 5, 10, 25, ...), meant for milliseconds,
    // and every confirmation falls in the first bucket.
    private static readonly IReadOnlyList<double> DurationBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

    private const string RoutingKeyTag = "routing_key";
    private const string OperationTag = "operation";
    private const string OutcomeTag = "outcome";

    private readonly Counter<long> _published;
    private readonly Counter<long> _publishFailures;
    private readonly Counter<long> _duplicatesRejected;
    private readonly Counter<long> _processed;
    private readonly Counter<long> _errorsRegistered;
    private readonly Histogram<double> _publishDuration;

    /// <summary>Initializes a new instance of the <see cref="PublisherMetrics"/> class.</summary>
    /// <param name="meterFactory">Factory that creates the meter, owned by the dependency injection container.</param>
    public PublisherMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);

        _published = meter.CreateCounter<long>("publisher.messages.published", "{message}", "Messages published to RabbitMQ and confirmed by the broker.");
        _publishFailures = meter.CreateCounter<long>("publisher.messages.publish_failures", "{message}", "Messages that RabbitMQ could not publish or confirm.");
        _duplicatesRejected = meter.CreateCounter<long>("publisher.messages.duplicates_rejected", "{message}", "Publish requests rejected because the same message already exists.");
        _processed = meter.CreateCounter<long>("publisher.messages.processed", "{message}", "Messages reported as processed by a consumer.");
        _errorsRegistered = meter.CreateCounter<long>("publisher.errors.registered", "{error}", "Errors reported for messages.");
        _publishDuration = meter.CreateHistogram("publisher.messages.publish.duration", "s", "Time RabbitMQ took to accept and confirm a message.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = DurationBucketBoundaries });
    }

    /// <summary>Records a message published and confirmed by the broker.</summary>
    /// <param name="routingKey">Routing key of the message.</param>
    /// <param name="operation"><see cref="PublishOperation"/> or <see cref="RepublishOperation"/>.</param>
    /// <param name="duration">Time the broker took to confirm the message.</param>
    public void MessagePublished(string routingKey, string operation, TimeSpan duration)
    {
        _published.Add(1, new(RoutingKeyTag, routingKey), new(OperationTag, operation));
        _publishDuration.Record(duration.TotalSeconds, new(RoutingKeyTag, routingKey), new(OperationTag, operation), new(OutcomeTag, "success"));
    }

    /// <summary>Records a message the broker could not publish or confirm.</summary>
    /// <param name="routingKey">Routing key of the message.</param>
    /// <param name="operation"><see cref="PublishOperation"/> or <see cref="RepublishOperation"/>.</param>
    /// <param name="duration">Time until the publication failed.</param>
    public void PublishFailed(string routingKey, string operation, TimeSpan duration)
    {
        _publishFailures.Add(1, new(RoutingKeyTag, routingKey), new(OperationTag, operation));
        _publishDuration.Record(duration.TotalSeconds, new(RoutingKeyTag, routingKey), new(OperationTag, operation), new(OutcomeTag, "failure"));
    }

    /// <summary>Records a publish request rejected as duplicate.</summary>
    /// <param name="routingKey">Routing key of the message.</param>
    public void DuplicateRejected(string routingKey)
        => _duplicatesRejected.Add(1, new KeyValuePair<string, object?>(RoutingKeyTag, routingKey));

    /// <summary>Records a message reported as processed.</summary>
    /// <param name="routingKey">Routing key of the message.</param>
    public void MessageProcessed(string routingKey)
        => _processed.Add(1, new KeyValuePair<string, object?>(RoutingKeyTag, routingKey));

    /// <summary>Records an error reported for a message.</summary>
    public void ErrorRegistered()
        => _errorsRegistered.Add(1);
}
