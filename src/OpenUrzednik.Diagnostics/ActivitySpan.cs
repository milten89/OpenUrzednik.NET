using System.Diagnostics;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Diagnostics;

/// <summary>
/// An <see cref="IOpenUrzednikSpan"/> over a started <see cref="Activity"/>; disposing it stops the activity.
/// </summary>
internal sealed class ActivitySpan : IOpenUrzednikSpan
{
    // OpenTelemetry semantic conventions for exceptions.
    internal const string ExceptionEventName = "exception";
    internal const string ExceptionTypeTag = "exception.type";
    internal const string ExceptionMessageTag = "exception.message";
    internal const string ExceptionStackTraceTag = "exception.stacktrace";

    private readonly Activity _activity;

    internal ActivitySpan(Activity activity) => _activity = activity;

    internal Activity Activity => _activity;

    public bool IsRecording => _activity.IsAllDataRequested;

    public void SetTag<T>(string key, T value) => _activity.SetTag(key, value);

    public void SetStatus(OpenUrzednikSpanStatus status, string? description = null)
        => _activity.SetStatus(status switch
        {
            OpenUrzednikSpanStatus.Ok => ActivityStatusCode.Ok,
            OpenUrzednikSpanStatus.Error => ActivityStatusCode.Error,
            _ => ActivityStatusCode.Unset,
        }, description);

    public void RecordException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _activity.AddEvent(new ActivityEvent(ExceptionEventName, tags: new ActivityTagsCollection
        {
            { ExceptionTypeTag, exception.GetType().FullName },
            { ExceptionMessageTag, exception.Message },
            { ExceptionStackTraceTag, exception.ToString() },
        }));
    }

    public void AddEvent(string name) => _activity.AddEvent(new ActivityEvent(name));

    public void AddEvent<T>(string name, string tagKey, T tagValue)
        => _activity.AddEvent(new ActivityEvent(name, tags: new ActivityTagsCollection { { tagKey, tagValue } }));

    public void Dispose() => _activity.Dispose();
}
