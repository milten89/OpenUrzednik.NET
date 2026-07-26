namespace OpenUrzednik.Core.Telemetry;

public sealed class NullOpenUrzednikLogger : IOpenUrzednikLogger
{
    public bool IsEnabled(OpenUrzednikLogLevel level) => false;

    public void Log(OpenUrzednikLogLevel level, string messageTemplate) { }
    public void Log<T1>(OpenUrzednikLogLevel level, string messageTemplate, T1 arg1) { }
    public void Log<T1, T2>(OpenUrzednikLogLevel level, string messageTemplate, T1 arg1, T2 arg2) { }
    public void Log<T1, T2, T3>(OpenUrzednikLogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3) { }
}