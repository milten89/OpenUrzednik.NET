using Microsoft.Extensions.Logging;

namespace OpenUrzednik.Extensions.Logging.Tests;

/// <summary>An <see cref="ILogger"/> that keeps every entry at or above <see cref="MinimumLevel"/>.</summary>
internal sealed class RecordingLogger : ILogger
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    public List<LogEntry> Entries { get; } = [];

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= MinimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add(new LogEntry(logLevel, eventId, (IReadOnlyList<KeyValuePair<string, object?>>)state!, exception, formatter(state, exception)));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

internal sealed record LogEntry(LogLevel Level, EventId EventId, IReadOnlyList<KeyValuePair<string, object?>> State, Exception? Exception, string Message);
