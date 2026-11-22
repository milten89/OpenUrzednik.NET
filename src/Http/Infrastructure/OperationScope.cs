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
/// span's own duration when the span can report it (<see cref="IOpenUrzednikSpanTiming"/>), else from the client's
/// <see cref="TimeProvider"/> timestamp taken at the start. An operation disposed without <see cref="Complete{T}"/> (it threw,
/// e.g. the caller cancelled) is recorded with <c>error.type</c> <see cref="OtherErrorType"/>, and its span gets the same tag and the
/// <c>Error</c> status (ADR-0025: every failure). With the metric disabled and a span nobody records, the scope holds only the span
/// and allocates nothing.
/// </remarks>
internal readonly struct OperationScope : IDisposable
{
    /// <summary>OpenTelemetry's <c>error.type</c> when the error isn't known: the operation threw before it completed.</summary>
    internal const string OtherErrorType = "_OTHER";

    // Only the span and the completion: the scope lives across the operation's await in every client call, so it stays small.
    // The completion is the measurement when the metric is enabled, a bare OperationCompletion when only the span records, else null.
    private readonly OperationCompletion? _completion;

    /// <summary>A scope that only holds the span: the metric is disabled and the span isn't recording.</summary>
    internal OperationScope(IOpenUrzednikSpan span) => Span = span;

    /// <summary>A scope that tracks its completion, and measures the operation when <paramref name="completion"/> is a <see cref="OperationMeasurement"/>.</summary>
    internal OperationScope(IOpenUrzednikSpan span, OperationCompletion completion)
    {
        Span = span;
        _completion = completion;
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

    /// <summary>
    /// If the operation didn't complete, records it with <see cref="OtherErrorType"/> and marks its span as failed with the same
    /// <c>error.type</c>; then ends the span.
    /// </summary>
    public void Dispose()
    {
        var completion = _completion;
        if (completion is not null && !completion.IsCompleted)
        {
            completion.Record(Span, OtherErrorType);
            Span.SetTag("error.type", OtherErrorType);
            Span.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
        }

        Span.Dispose();
    }

    private void Record(string? errorType) => _completion?.Record(Span, errorType);
}
