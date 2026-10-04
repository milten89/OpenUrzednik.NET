using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Extensions.Logging;

/// <summary>
/// An <see cref="IOpenUrzednikLogger"/> that writes to a Microsoft.Extensions.Logging <see cref="ILogger"/>.
/// </summary>
/// <remarks>
/// Entries stay structured: the state lists the named values followed by <c>{OriginalFormat}</c>, as <c>LoggerMessage</c> does,
/// so providers such as Serilog or OpenTelemetry see the template and its properties. The message is formatted with the invariant culture.
/// Use <see cref="OpenUrzednikLoggerFactoryExtensions.CreateOpenUrzednikLogger{TClient}(ILoggerFactory)"/> to get one per client,
/// with the client's full type name as the category (e.g. <c>OpenUrzednik.Nbp.Gold.NbpGoldPriceClient</c>).
/// </remarks>
public sealed class OpenUrzednikLogger : IOpenUrzednikLogger
{
    private static readonly Func<LogValues, Exception?, string> Formatter = static (state, _) => state.ToString();

    private readonly ILogger _logger;

    /// <summary>Creates an adapter that writes to <paramref name="logger"/>.</summary>
    /// <param name="logger">The logger the entries are written to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public OpenUrzednikLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsEnabled(OpenUrzednikLogLevel level) => _logger.IsEnabled(ToLogLevel(level));

    /// <inheritdoc/>
    public void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate)
    {
        if (TryGetEnabledLevel(level, out var logLevel))
            Write(logLevel, exception, messageTemplate, []);
    }

    /// <inheritdoc/>
    public void Log<T0>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                        string name0, T0 value0)
    {
        if (TryGetEnabledLevel(level, out var logLevel))
            Write(logLevel, exception, messageTemplate, [new(name0, value0)]);
    }

    /// <inheritdoc/>
    public void Log<T0, T1>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                            string name0, T0 value0,
                            string name1, T1 value1)
    {
        if (TryGetEnabledLevel(level, out var logLevel))
            Write(logLevel, exception, messageTemplate, [new(name0, value0), new(name1, value1)]);
    }

    /// <inheritdoc/>
    public void Log<T0, T1, T2>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                                string name0, T0 value0,
                                string name1, T1 value1,
                                string name2, T2 value2)
    {
        if (TryGetEnabledLevel(level, out var logLevel))
            Write(logLevel, exception, messageTemplate, [new(name0, value0), new(name1, value1), new(name2, value2)]);
    }

    /// <inheritdoc/>
    public void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                    IReadOnlyList<KeyValuePair<string, object?>> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (TryGetEnabledLevel(level, out var logLevel))
            Write(logLevel, exception, messageTemplate, [.. properties]);
    }

    private bool TryGetEnabledLevel(OpenUrzednikLogLevel level, out LogLevel logLevel)
    {
        logLevel = ToLogLevel(level);
        return _logger.IsEnabled(logLevel);
    }

    [SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Every caller checks IsEnabled (TryGetEnabledLevel) before it builds the values.")]
    private void Write(LogLevel logLevel, Exception? exception, string messageTemplate, KeyValuePair<string, object?>[] values)
        => _logger.Log(logLevel, default, new LogValues(messageTemplate, values), exception, Formatter);

    internal static LogLevel ToLogLevel(OpenUrzednikLogLevel level) => level switch
    {
        OpenUrzednikLogLevel.Trace => LogLevel.Trace,
        OpenUrzednikLogLevel.Debug => LogLevel.Debug,
        OpenUrzednikLogLevel.Information => LogLevel.Information,
        OpenUrzednikLogLevel.Warning => LogLevel.Warning,
        OpenUrzednikLogLevel.Error => LogLevel.Error,
        OpenUrzednikLogLevel.Critical => LogLevel.Critical,
        _ => LogLevel.None,
    };
}
