namespace OpenUrzednik.Core.Telemetry;

public interface IOpenUrzednikLogger
{
    bool IsEnabled(OpenUrzednikLogLevel level);

    void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate);
    void Log<T0>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                 string name0, T0 value0);
    void Log<T0, T1>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                     string name0, T0 value0,
                     string name1, T1 value1);
    void Log<T0, T1, T2>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                         string name0, T0 value0,
                         string name1, T1 value1,
                         string name2, T2 value2);

    void Log(OpenUrzednikLogLevel level, string messageTemplate, Exception? exception,
             IReadOnlyList<KeyValuePair<string, object?>> properties);
}
