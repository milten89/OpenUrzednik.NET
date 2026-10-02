namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// A started span; disposing it ends the span. Started by <see cref="IOpenUrzednikTraceSource.StartSpan"/>.
/// </summary>
public interface IOpenUrzednikSpan : IDisposable
{
    /// <summary>
    /// Whether tags, events and status are kept. When <see langword="false"/>, callers may skip computing them.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>Sets a tag, e.g. <c>nbp.top_count</c> or <c>http.response.status_code</c>.</summary>
    /// <param name="key">The tag name: <c>&lt;provider&gt;.&lt;parameter&gt;</c>, or an OpenTelemetry semantic-convention name.</param>
    /// <param name="value">The tag value.</param>
    void SetTag<T>(string key, T value);

    /// <summary>Sets the span status.</summary>
    /// <param name="status">The status.</param>
    /// <param name="description">A description, kept for <see cref="OpenUrzednikSpanStatus.Error"/>.</param>
    void SetStatus(OpenUrzednikSpanStatus status, string? description = null);

    /// <summary>Records an exception as an event on the span.</summary>
    /// <param name="exception">The exception.</param>
    void RecordException(Exception exception);

    /// <summary>Adds an event without tags.</summary>
    /// <param name="name">The event name.</param>
    void AddEvent(string name);

    /// <summary>Adds an event with one tag, e.g. <c>error</c> with <c>error.code</c>.</summary>
    /// <param name="name">The event name.</param>
    /// <param name="tagKey">The tag name.</param>
    /// <param name="tagValue">The tag value.</param>
    void AddEvent<T>(string name, string tagKey, T tagValue);
}
