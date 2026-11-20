using System.Diagnostics;

namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// An <see cref="IOpenUrzednikSpan"/> over a started <see cref="Activity"/>; disposing it stops the activity.
/// Compiled into Core on the .NET targets and linked into <c>OpenUrzednik.Diagnostics</c> for netstandard2.0, so both use one implementation (ADR-0025).
/// </summary>
internal sealed class ActivitySpan : IOpenUrzednikSpan, IOpenUrzednikSpanTiming
{
    // OpenTelemetry semantic conventions for exceptions.
    internal const string ExceptionEventName = "exception";
    internal const string ExceptionTypeTag = "exception.type";
    internal const string ExceptionMessageTag = "exception.message";
    internal const string ExceptionStackTraceTag = "exception.stacktrace";

    private readonly Activity _activity;
    // When the span started, as a Stopwatch timestamp: StopTiming sets the end time from the elapsed
    // Stopwatch time, not from DateTime.UtcNow, which moves in ~15.6 ms steps on .NET Framework while the activity's start
    // time is precise there.
    private readonly long _startTimestamp;

    internal ActivitySpan(Activity activity)
    {
        _activity = activity;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

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

    /// <inheritdoc/>
    /// <remarks>
    /// Sets the activity's end time to its start time plus the elapsed <see cref="Stopwatch"/> time, so
    /// <see cref="Activity.Duration"/> is final and never negative; <see cref="Activity.Stop"/> keeps an end time that is
    /// already set, so the trace shows the same duration. A later call moves the end time again.
    /// </remarks>
    public TimeSpan? StopTiming()
    {
        _activity.SetEndTime(_activity.StartTimeUtc + Elapsed());
        return _activity.Duration;
    }

    public void Dispose() => _activity.Dispose();

#if NET
    private TimeSpan Elapsed() => Stopwatch.GetElapsedTime(_startTimestamp);
#else
    // netstandard2.0 (linked into OpenUrzednik.Diagnostics) has no Stopwatch.GetElapsedTime.
    private static readonly double TicksPerTimestamp = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

    private TimeSpan Elapsed() => TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - _startTimestamp) * TicksPerTimestamp));
#endif
}
