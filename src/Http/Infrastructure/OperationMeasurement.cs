using System.Collections;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// The duration of one finished client operation (one public method call), recorded on the histogram
/// <c>openurzednik.client.operation.duration</c> (ADR-0025). Its name, unit, description, bucket advice and tags are set
/// here, where the operation is measured; the packages' telemetry references document them.
/// </summary>
/// <remarks>
/// Created when the operation starts (<see cref="OperationScope"/>, only while the metric is enabled), so everything the
/// measurement needs lives here and the scope stays small; completed and recorded once when the operation ends. It is its own tag list, so recording an operation allocates this one object; the boxed port comes from the caller,
/// which boxes it once per client. Tags never carry identifiers: the operation is a span name and the error type an
/// error code.
/// </remarks>
internal sealed class OperationMeasurement : OperationCompletion, IOpenUrzednikHistogramMeasurement, IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <summary>The instrument's name.</summary>
    internal const string InstrumentName = "openurzednik.client.operation.duration";

    // OpenTelemetry's advice for RPC client call durations (rpc.client.call.duration): 0.1 ms to 60 s, because a public
    // API can take long. Shared, never changed.
    private static readonly double[] Boundaries =
        [0.0001, 0.0005, 0.001, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10, 15, 30, 45, 60];

    private readonly string _operation;
    private readonly string _provider;
    private string? _errorType;
    private readonly string? _serverAddress;
    private readonly object? _serverPort;
    private readonly IOpenUrzednikMetrics _metrics;
    private readonly TimeProvider _timeProvider;
    private readonly long _startTimestamp;

    /// <param name="operation">The operation's span name, e.g. <c>nbp.gold.latest</c>.</param>
    /// <param name="provider">The provider's short name, e.g. <c>nbp</c>.</param>
    /// <param name="serverAddress">The API's host (<c>server.address</c>), if known.</param>
    /// <param name="serverPort">The API's port (<c>server.port</c>), boxed once by the caller, if known.</param>
    /// <param name="metrics">Where <see cref="Record"/> records the measurement.</param>
    /// <param name="timeProvider">The clock for an operation whose span can't report its duration; the start is read now.</param>
    internal OperationMeasurement(string operation, string provider, string? serverAddress, object? serverPort,
        IOpenUrzednikMetrics metrics, TimeProvider timeProvider)
    {
        _operation = operation;
        _provider = provider;
        _serverAddress = serverAddress;
        _serverPort = serverPort;
        _metrics = metrics;
        _timeProvider = timeProvider;
        _startTimestamp = timeProvider.GetTimestamp();
    }

    /// <summary>
    /// Completes the measurement, once, and records it: the span's own duration when it can report one
    /// (<see cref="IOpenUrzednikSpanTiming"/>), else the time elapsed since the start. Later calls do nothing.
    /// </summary>
    /// <param name="span">The operation's span, still open.</param>
    /// <param name="errorType">The error code when it failed (OpenTelemetry's <c>error.type</c>); <see langword="null"/> on success.</param>
    internal override void Record(IOpenUrzednikSpan span, string? errorType)
    {
        if (IsCompleted)
        {
            return;
        }

        var spanDuration = (span as IOpenUrzednikSpanTiming)?.StopTiming();
        Complete(spanDuration.HasValue ? spanDuration.GetValueOrDefault() : _timeProvider.GetElapsedTime(_startTimestamp), errorType);
        _metrics.Record(this);
    }

    /// <summary>Sets the operation's duration and error type.</summary>
    /// <param name="duration">How long the operation took.</param>
    /// <param name="errorType">The error code when it failed (OpenTelemetry's <c>error.type</c>); <see langword="null"/> on success.</param>
    internal void Complete(TimeSpan duration, string? errorType)
    {
        Value = duration.TotalSeconds;
        _errorType = errorType;
        IsCompleted = true;
    }

    /// <inheritdoc/>
    public string Name => InstrumentName;

    /// <inheritdoc/>
    public string Unit => "s";

    /// <inheritdoc/>
    public string Description => "Duration of OpenUrzednik.NET client operations.";

    /// <inheritdoc/>
    public double Value { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<double>? BucketBoundaries => Boundaries;

    /// <inheritdoc/>
    /// <remarks>In this order: <c>openurzednik.operation</c>, <c>openurzednik.provider</c>, then <c>error.type</c>,
    /// <c>server.address</c> and <c>server.port</c> when they have a value.</remarks>
    public IReadOnlyList<KeyValuePair<string, object?>> Tags => this;

    /// <inheritdoc/>
    public int Count => 2 + (_errorType is null ? 0 : 1) + (_serverAddress is null ? 0 : 1) + (_serverPort is null ? 0 : 1);

    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (index is 0 or 1)
            {
                return Slot(index);
            }

            // The optional tags (slots 2 to 4), in order, skipping those without a value.
            var remaining = index - 2;
            for (var slot = 2; remaining >= 0 && slot < 5; slot++)
            {
                var tag = Slot(slot);
                if (tag.Value is not null && remaining-- == 0)
                {
                    return tag;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private KeyValuePair<string, object?> Slot(int slot) => slot switch
    {
        0 => new("openurzednik.operation", _operation),
        1 => new("openurzednik.provider", _provider),
        2 => new("error.type", _errorType),
        3 => new("server.address", _serverAddress),
        _ => new("server.port", _serverPort),
    };
}
