using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Whether a client operation completed (<see cref="OperationScope.Complete{T}"/> ran), so that a scope disposed without it can mark
/// its span as failed. The scope creates one only when there is something to mark or measure: a recording span, or the metric
/// (<see cref="OperationMeasurement"/>, which also records the duration).
/// </summary>
internal class OperationCompletion
{
    /// <summary>Whether the operation is complete.</summary>
    internal bool IsCompleted { get; private protected set; }

    /// <summary>Completes the operation; a <see cref="OperationMeasurement"/> also records it, once.</summary>
    /// <param name="span">The operation's span, still open.</param>
    /// <param name="errorType">The error code when it failed (OpenTelemetry's <c>error.type</c>); <see langword="null"/> on success.</param>
    internal virtual void Record(IOpenUrzednikSpan span, string? errorType) => IsCompleted = true;
}
