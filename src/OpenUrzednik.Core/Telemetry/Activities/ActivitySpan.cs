using System.Diagnostics;

namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// An <see cref="IOpenUrzednikSpan"/> over a started <see cref="Activity"/>; disposing it stops the activity.
/// Compiled into Core on the .NET targets and linked into <c>OpenUrzednik.Diagnostics</c> for netstandard2.0, so both use one implementation (ADR-0003).
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

    /// <summary>
    /// Starts an <see cref="ActivityKind.Internal"/> activity on <paramref name="source"/>. When nothing listens to the source,
    /// or the span isn't sampled, returns the no-op span and allocates nothing.
    /// </summary>
    /// <remarks>OpenUrzednik.Diagnostics calls it through <c>InternalsVisibleTo</c> on .NET: within a major version, don't change or remove it.</remarks>
    internal static IOpenUrzednikSpan Start(ActivitySource source, string operationName)
    {
        var activity = source.StartActivity(operationName, ActivityKind.Internal);
        return activity is null ? NullOpenUrzednikTraceSource.Instance.StartSpan(operationName) : new ActivitySpan(activity);
    }

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
