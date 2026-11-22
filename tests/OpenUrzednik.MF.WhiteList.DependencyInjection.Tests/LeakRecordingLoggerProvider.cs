using Microsoft.Extensions.Logging;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

/// <summary>
/// An <see cref="ILoggerProvider"/> that keeps, for every entry and every scope, the texts a log sink could write: the formatted message,
/// the exception, and each key and value of the structured state. For the leak tests (ADR-0017).
/// </summary>
internal sealed class LeakRecordingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly List<LeakEntry> _entries = [];
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public IReadOnlyList<LeakEntry> Entries
    {
        get
        {
            lock (_entries)
                return [.. _entries];
        }
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose() { }

    private void Add(LeakEntry entry)
    {
        lock (_entries)
            _entries.Add(entry);
    }

    private static void AddState(List<string> texts, object? state)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (var pair in pairs)
            {
                texts.Add(pair.Key);
                if (pair.Value is not null)
                    texts.Add(pair.Value.ToString() ?? string.Empty);
            }
        }
        else if (state is not null)
        {
            texts.Add(state.ToString() ?? string.Empty);
        }
    }

    private sealed class Logger(LeakRecordingLoggerProvider provider, string category) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var texts = new List<string> { formatter(state, exception) };
            AddState(texts, state);
            if (exception is not null)
                texts.Add(exception.ToString());
            // The scopes open around this entry, through the scope provider the logger factory hands to this provider.
            provider._scopes.ForEachScope(static (scope, collected) => AddState(collected, scope), texts);
            provider.Add(new LeakEntry(category, texts));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes.Push(state);
    }
}

internal sealed record LeakEntry(string Category, IReadOnlyList<string> Texts);
