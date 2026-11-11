namespace RabbitMQPublisher.Test.Common;

/// <summary>
/// Meter factory for tests. Each instance tags its meters with itself as scope, so a
/// <see cref="MetricCollector{T}"/> created with the same scope only sees the measurements of one test.
/// </summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        Meter meter = new(new MeterOptions(options.Name) { Version = options.Version, Tags = options.Tags, Scope = this });
        _meters.Add(meter);
        return meter;
    }

    public MetricCollector<T> CreateCollector<T>(string instrumentName) where T : struct
        => new(this, PublisherMetrics.MeterName, instrumentName);

    public void Dispose()
    {
        foreach (Meter meter in _meters)
            meter.Dispose();
    }
}
