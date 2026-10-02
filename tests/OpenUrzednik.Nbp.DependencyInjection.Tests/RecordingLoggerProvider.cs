using Microsoft.Extensions.Logging;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

/// <summary>An <see cref="ILoggerProvider"/> that keeps every entry with its category.</summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_entries)
                return [.. _entries];
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Dispose() { }

    private void Add(LogEntry entry)
    {
        lock (_entries)
            _entries.Add(entry);
    }

    private sealed class Logger(RecordingLoggerProvider provider, string category) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => provider.Add(new LogEntry(category, logLevel, formatter(state, exception)));

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}

internal sealed record LogEntry(string Category, LogLevel Level, string Message);
