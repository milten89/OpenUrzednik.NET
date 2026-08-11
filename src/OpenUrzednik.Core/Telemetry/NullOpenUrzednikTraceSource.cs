using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Telemetry;

public sealed class NullOpenUrzednikTraceSource : IOpenUrzednikTraceSource
{
    private static readonly NullOpenUrzednikSpan SpanInstance = new();

    public static readonly NullOpenUrzednikTraceSource Instance = new();

    public IOpenUrzednikSpan StartSpan(string operationName)
        => SpanInstance;

    private sealed class NullOpenUrzednikSpan : IOpenUrzednikSpan
    {
        public bool IsRecording => false;
        public void SetTag<T>(string key, T value) { }

        public void SetStatus(SpanStatus status, string? description = null) { }

        public void RecordException(Exception exception) { }

        public void AddEvent(string name) { }

        public void AddEvent<T>(string name, string tagKey, T tagValue) { }

        public void Dispose() { }
    }
}
