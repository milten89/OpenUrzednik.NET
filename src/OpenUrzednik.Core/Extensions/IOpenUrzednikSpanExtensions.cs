using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Core.Extensions;

public static class IOpenUrzednikSpanExtensions
{
    public static void RecordError(this IOpenUrzednikSpan span, OpenUrzednikError error)
    {
        if (!span.IsRecording) return;
        span.SetTag("error.code", error.Code);
        span.SetStatus(OpenUrzednikSpanStatus.Error, error.Message);
    }

    public static void RecordErrors(this IOpenUrzednikSpan span, IReadOnlyList<OpenUrzednikError> errors)
    {
        if (!span.IsRecording || errors.Count == 0) return;

        foreach (var error in errors)
            span.AddEvent("error", "error.code", error.Code);

        span.SetStatus(OpenUrzednikSpanStatus.Error,
            errors.Count == 1 ? errors[0].Message : $"{errors.Count} errors occurred");
    }
}
