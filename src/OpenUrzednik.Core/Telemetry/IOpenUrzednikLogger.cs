namespace OpenUrzednik.Core.Telemetry;

public interface IOpenUrzednikLogger
{
    bool IsEnabled(OpenUrzednikLogLevel level);

    void Log(OpenUrzednikLogLevel level, string messageTemplate);
    void Log<T1>(OpenUrzednikLogLevel level, string messageTemplate, T1 arg1);
    void Log<T1, T2>(OpenUrzednikLogLevel level, string messageTemplate, T1 arg1, T2 arg2);
    void Log<T1, T2, T3>(OpenUrzednikLogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3);
}
