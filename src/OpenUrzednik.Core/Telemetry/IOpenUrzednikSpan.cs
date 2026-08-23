namespace OpenUrzednik.Core.Telemetry;

public interface IOpenUrzednikSpan : IDisposable
{
    bool IsRecording { get; }

    void SetTag<T>(string key, T value);
    void SetStatus(OpenUrzednikSpanStatus status, string? description = null);
    void RecordException(Exception exception);
    void AddEvent(string name);
    void AddEvent<T>(string name, string tagKey, T tagValue);
}
