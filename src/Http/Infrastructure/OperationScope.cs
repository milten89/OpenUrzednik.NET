using OpenUrzednik.Core;
using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// One client operation (one public method call): its span, and its measurement on
/// <c>openurzednik.client.operation.duration</c> while that metric is enabled (ADR-0025). A client method starts it with
/// <see cref="RestRequestExecutor.StartOperation"/> (through its provider's request pipeline), uses <see cref="Span"/> as its
/// trace span, returns <see cref="Complete{T}"/> of its result, and disposes it:
/// <code>
/// using var operation = _pipeline.StartOperation("nbp.gold.latest");
/// return operation.Complete(await _pipeline.GetFirstAsync(operation.Span, ...).ConfigureAwait(false));
/// </code>
/// </summary>
/// <remarks>
/// The operation is timed once and recorded while its span is still current, so a metric can link to its trace: with the
/// span's own duration when the span can report it (<see cref="IOpenUrzednikSpanTiming"/>), else from the
/// <see cref="TimeProvider"/> timestamp taken at the start. An operation disposed without <see cref="Complete{T}"/> (it threw,
/// e.g. the caller cancelled) is recorded with <c>error.type</c> <see cref="OtherErrorType"/>. With the metric disabled the
/// scope holds only the span and allocates nothing.
/// </remarks>
internal readonly struct OperationScope : IDisposable
{
    /// <summary>OpenTelemetry's <c>error.type</c> when the error isn't known: the operation threw before it completed.</summary>
    internal const string OtherErrorType = "_OTHER";

    private readonly IOpenUrzednikMetrics? _metrics;
    private readonly OperationMeasurement? _measurement;
    private readonly TimeProvider? _timeProvider;
    private readonly long _startTimestamp;

    /// <summary>A scope that only holds the span: the metric is disabled.</summary>
    internal OperationScope(IOpenUrzednikSpan span) => Span = span;

    /// <summary>A scope that measures the operation.</summary>
    internal OperationScope(IOpenUrzednikSpan span, IOpenUrzednikMetrics metrics, OperationMeasurement measurement, TimeProvider timeProvider)
    {
        Span = span;
        _metrics = metrics;
        _measurement = measurement;
        _timeProvider = timeProvider;
        _startTimestamp = timeProvider.GetTimestamp();
    }

    /// <summary>The operation's trace span.</summary>
    internal IOpenUrzednikSpan Span { get; }

    /// <summary>
    /// Records the operation with the result's first error code as <c>error.type</c> (none on success), once, and returns the
    /// result unchanged.
    /// </summary>
    internal OpenUrzednikResult<T> Complete<T>(OpenUrzednikResult<T> result)
    {
        Record(result.IsFailure ? result.Errors[0].Code : null);
        return result;
    }

    /// <summary>Records the operation with <see cref="OtherErrorType"/> if it didn't complete, then ends the span.</summary>
    public void Dispose()
    {
        Record(OtherErrorType);
        Span.Dispose();
    }

    private void Record(string? errorType)
    {
        if (_measurement is null || _measurement.IsCompleted)
        {
            return;
        }

        var duration = (Span as IOpenUrzednikSpanTiming)?.StopTiming() ?? _timeProvider!.GetElapsedTime(_startTimestamp);
        _measurement.Complete(duration, errorType);
        _metrics!.Record(_measurement);
    }
}
