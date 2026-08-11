namespace OpenUrzednik.Core.Telemetry;

public sealed class NullOpenUrzednikLogger : IOpenUrzednikLogger
{
    public static readonly NullOpenUrzednikLogger Instance = new();

    public bool IsEnabled(OpenUrzednikLogLevel level) => false;
    public void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate) { }

    public void Log<T0>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                        string name0, T0 value0) { }

    public void Log<T0, T1>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                            string name0, T0 value0,
                            string name1, T1 value1) { }

    public void Log<T0, T1, T2>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                                string name0, T0 value0,
                                string name1, T2 value1,
                                string name2, T2 value2) { }

    public void Log(OpenUrzednikLogLevel level, string messageTemplate, Exception? exception,
                    IReadOnlyList<KeyValuePair<string, object?>> properties) { }

}
