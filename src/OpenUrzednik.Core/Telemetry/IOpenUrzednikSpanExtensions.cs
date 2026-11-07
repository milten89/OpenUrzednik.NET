using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Records <see cref="OpenUrzednikError"/>s on an <see cref="IOpenUrzednikSpan"/>.
/// </summary>
public static class IOpenUrzednikSpanExtensions
{
    /// <summary>
    /// Marks the span as failed with the error's message and sets the OpenTelemetry <c>error.type</c> tag to <see cref="OpenUrzednikError.Code"/>.
    /// Does nothing when the span isn't recording.
    /// </summary>
    /// <param name="span">Span to update.</param>
    /// <param name="error">Error to record.</param>
    public static void RecordError(this IOpenUrzednikSpan span, OpenUrzednikError error)
    {
        if (!span.IsRecording) return;
        span.SetTag("error.type", error.Code);
        span.SetStatus(OpenUrzednikSpanStatus.Error, error.Message);
    }

    /// <summary>
    /// Sets the <c>error.type</c> tag to the first error's code, adds an <c>error</c> event with the <c>error.type</c> tag per error,
    /// and marks the span as failed: with the error's message for one error, or with the number of errors for several.
    /// Does nothing when the span isn't recording or the list is empty.
    /// </summary>
    /// <remarks>The <c>error</c> events are best-effort: unlike the tag, they aren't part of the telemetry contract (ADR-0003).</remarks>
    /// <param name="span">Span to update.</param>
    /// <param name="errors">Errors to record.</param>
    public static void RecordErrors(this IOpenUrzednikSpan span, IReadOnlyList<OpenUrzednikError> errors)
    {
        if (!span.IsRecording) return;
        // A local, not `errors.Count` in the comparisons, so that mutation testing checks them (ADR-0018): Stryker's LINQ mutator
        // turns the property into `errors.Sum`, which can't compile, and discards the mutants around it.
        var count = errors.Count;
        if (count == 0) return;

        span.SetTag("error.type", errors[0].Code);
        foreach (var error in errors)
            span.AddEvent("error", "error.type", error.Code);

        span.SetStatus(OpenUrzednikSpanStatus.Error,
            count == 1 ? errors[0].Message : $"{count} errors occurred");
    }
}
