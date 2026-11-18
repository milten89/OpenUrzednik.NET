using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.TestCommon;

/// <summary>
/// A logger and trace source that keep everything a client writes, with every level enabled and every span recording, so a test
/// can search it (<see cref="IdentifierLeak"/>). Values are kept as text, formatted invariantly; collections are
/// flattened.
/// </summary>
public sealed class RecordingTelemetry : IOpenUrzednikLogger, IOpenUrzednikTraceSource
{
    private readonly List<(string Place, string Text)> _texts = [];
    private readonly List<(string Place, Exception Exception)> _exceptions = [];
    private readonly object _lock = new();

    /// <summary>Every piece of text written: where it went (<c>log template</c>, <c>span tag url.path</c>, …) and the text.</summary>
    public IReadOnlyList<(string Place, string Text)> Texts
    {
        get
        {
            lock (_lock)
                return [.. _texts];
        }
    }

    /// <summary>Every exception logged or recorded on a span, with where it went.</summary>
    public IReadOnlyList<(string Place, Exception Exception)> Exceptions
    {
        get
        {
            lock (_lock)
                return [.. _exceptions];
        }
    }

    /// <inheritdoc />
    public bool IsEnabled(OpenUrzednikLogLevel level) => true;

    /// <inheritdoc />
    public void Log(OpenUrzednikLogLevel level, OpenUrzednikEventId eventId, Exception? exception, string messageTemplate)
        => Log(level, eventId, exception, messageTemplate, []);

    /// <inheritdoc />
    public void Log<T0>(OpenUrzednikLogLevel level, OpenUrzednikEventId eventId, Exception? exception, string messageTemplate,
        string name0, T0 value0)
        => Log(level, eventId, exception, messageTemplate, [new(name0, value0)]);

    /// <inheritdoc />
    public void Log<T0, T1>(OpenUrzednikLogLevel level, OpenUrzednikEventId eventId, Exception? exception, string messageTemplate,
        string name0, T0 value0, string name1, T1 value1)
        => Log(level, eventId, exception, messageTemplate, [new(name0, value0), new(name1, value1)]);

    /// <inheritdoc />
    public void Log<T0, T1, T2>(OpenUrzednikLogLevel level, OpenUrzednikEventId eventId, Exception? exception, string messageTemplate,
        string name0, T0 value0, string name1, T1 value1, string name2, T2 value2)
        => Log(level, eventId, exception, messageTemplate, [new(name0, value0), new(name1, value1), new(name2, value2)]);

    /// <inheritdoc />
    public void Log(OpenUrzednikLogLevel level, OpenUrzednikEventId eventId, Exception? exception, string messageTemplate,
        IReadOnlyList<KeyValuePair<string, object?>> properties)
    {
        var place = $"log {level} {eventId.Id} {eventId.Name}";
        Add($"{place}: template", messageTemplate);
        foreach (var property in properties)
        {
            Add($"{place}: property name", property.Key);
            AddValue($"{place}: property {property.Key}", property.Value);
        }
        if (exception is not null)
            AddException($"{place}: exception", exception);
    }

    /// <inheritdoc />
    public IOpenUrzednikSpan StartSpan(string operationName)
    {
        Add("span name", operationName);
        return new Span(this, operationName);
    }

    internal void Add(string place, string? text)
    {
        if (text is null)
            return;
        lock (_lock)
            _texts.Add((place, text));
    }

    internal void AddValue(string place, object? value)
    {
        foreach (var text in IdentifierLeak.Flatten(value))
            Add(place, text);
    }

    internal void AddException(string place, Exception exception)
    {
        lock (_lock)
            _exceptions.Add((place, exception));
    }

    private sealed class Span(RecordingTelemetry telemetry, string spanName) : IOpenUrzednikSpan
    {
        public bool IsRecording => true;

        public void SetTag<T>(string key, T value)
        {
            telemetry.Add($"span {spanName}: tag key", key);
            telemetry.AddValue($"span {spanName}: tag {key}", value);
        }

        public void SetStatus(OpenUrzednikSpanStatus status, string? description = null)
            => telemetry.Add($"span {spanName}: status {status}", description);

        public void RecordException(Exception exception) => telemetry.AddException($"span {spanName}: exception", exception);

        public void AddEvent(string name) => telemetry.Add($"span {spanName}: event", name);

        public void AddEvent<T>(string name, string tagKey, T tagValue)
        {
            telemetry.Add($"span {spanName}: event", name);
            telemetry.Add($"span {spanName}: event {name} tag key", tagKey);
            telemetry.AddValue($"span {spanName}: event {name} tag {tagKey}", tagValue);
        }

        public void Dispose()
        {
        }
    }
}
