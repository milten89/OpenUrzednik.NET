namespace OpenUrzednik.Core.Telemetry;

public interface IOpenUrzednikTraceSource
{
    IOpenUrzednikSpan StartSpan(string operationName);
}
