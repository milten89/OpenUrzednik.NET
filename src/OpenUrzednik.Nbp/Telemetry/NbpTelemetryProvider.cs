using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Nbp.Telemetry;

internal sealed class NbpTelemetryProvider(IOpenUrzednikLogger? logger, IOpenUrzednikTraceSource? tracer)
{
    public IOpenUrzednikLogger Logger => logger ?? NullOpenUrzednikLogger.Instance;
    public IOpenUrzednikTraceSource Tracer => tracer ?? NullOpenUrzednikTraceSource.Instance;
}
