namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Logger that discards every message: the default when no logger is passed.
/// </summary>
public sealed class NullOpenUrzednikLogger : IOpenUrzednikLogger
{
    /// <summary>The shared instance.</summary>
    public static readonly NullOpenUrzednikLogger Instance = new();

    private NullOpenUrzednikLogger() { }

    /// <inheritdoc/>
    /// <returns>Always <see langword="false"/>.</returns>
    public bool IsEnabled(OpenUrzednikLogLevel level) => false;

    /// <inheritdoc/>
    public void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate)
    {
    }

    /// <inheritdoc/>
    public void Log<T0>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                        string name0, T0 value0)
    {
    }

    /// <inheritdoc/>
    public void Log<T0, T1>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                            string name0, T0 value0,
                            string name1, T1 value1)
    {
    }

    /// <inheritdoc/>
    public void Log<T0, T1, T2>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                                string name0, T0 value0,
                                string name1, T1 value1,
                                string name2, T2 value2)
    {
    }

    /// <inheritdoc/>
    public void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                    IReadOnlyList<KeyValuePair<string, object?>> properties)
    {
    }
}
