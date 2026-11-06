namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Trace source whose spans record nothing: the default when no trace source is passed.
/// </summary>
public sealed class NullOpenUrzednikTraceSource : IOpenUrzednikTraceSource
{
    private static readonly NullOpenUrzednikSpan SpanInstance = new();

    /// <summary>The shared instance.</summary>
    public static readonly NullOpenUrzednikTraceSource Instance = new();

    /// <inheritdoc/>
    /// <returns>A shared span that ignores every call and is never recording.</returns>
    public IOpenUrzednikSpan StartSpan(string operationName)
        => SpanInstance;

    private sealed class NullOpenUrzednikSpan : IOpenUrzednikSpan
    {
        public bool IsRecording => false;

        public void SetTag<T>(string key, T value) { }

        public void SetStatus(OpenUrzednikSpanStatus status, string? description = null) { }

        public void RecordException(Exception exception) { }

        public void AddEvent(string name) { }

        public void AddEvent<T>(string name, string tagKey, T tagValue) { }

        public void Dispose() { }
    }
}
