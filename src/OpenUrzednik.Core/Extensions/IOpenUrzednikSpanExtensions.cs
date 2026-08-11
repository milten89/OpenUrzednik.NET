using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Core.Extensions;

public static class IOpenUrzednikSpanExtensions
{
    public static void RecordError<TError>(this IOpenUrzednikSpan span, TError error)
        where TError : OpenUrzednikError
    {
        if (!span.IsRecording) return;
        span.SetTag("error.code", error.Code);
        span.SetStatus(SpanStatus.Error, error.Message);
    }

    public static void RecordErrors<TError>(this IOpenUrzednikSpan span, IReadOnlyList<TError> errors)
        where TError : OpenUrzednikError
    {
        if (!span.IsRecording || errors.Count == 0) return;

        foreach (var error in errors)
            span.AddEvent("error", "error.code", error.Code);

        span.SetStatus(SpanStatus.Error,
            errors.Count == 1 ? errors[0].Message : $"{errors.Count} errors occurred");
    }
}
