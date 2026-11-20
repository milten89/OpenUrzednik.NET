using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public class ClientTelemetryTest
{
    [Fact]
    public void Ctor_NoArguments_UsesNullImplementations()
    {
        // Act
        var telemetry = new ClientTelemetry();

        // Assert
        telemetry.Logger.ShouldBeSameAs(NullOpenUrzednikLogger.Instance);
        telemetry.TraceSource.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance);
        telemetry.Metrics.ShouldBeSameAs(NullOpenUrzednikMetrics.Instance);
    }

    [Fact]
    public void Ctor_SourceNameWithoutMetrics_UsesSharedMeterOnDotNet()
    {
        // Act
        var telemetry = new ClientTelemetry(sourceName: "OpenUrzednik.Tests.Fallback");

        // Assert
#if NET
        telemetry.Metrics.ShouldNotBeSameAs(NullOpenUrzednikMetrics.Instance);
        telemetry.Metrics.ShouldBeSameAs(new ClientTelemetry(sourceName: "OpenUrzednik.Tests.Fallback").Metrics);
#else
        telemetry.Metrics.ShouldBeSameAs(NullOpenUrzednikMetrics.Instance);
#endif
    }

    [Fact]
    public void Ctor_SourceNameWithoutTraceSource_UsesSharedSourceOnDotNet()
    {
        // Act
        var telemetry = new ClientTelemetry(sourceName: "OpenUrzednik.Tests.Fallback");

        // Assert
#if NET
        telemetry.TraceSource.ShouldNotBeSameAs(NullOpenUrzednikTraceSource.Instance);
        telemetry.TraceSource.ShouldBeSameAs(new ClientTelemetry(sourceName: "OpenUrzednik.Tests.Fallback").TraceSource);
#else
        telemetry.TraceSource.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance);
#endif
    }

    [Fact]
    public void Ctor_CustomComponents_UsesThem()
    {
        // Arrange
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var traceSource = Substitute.For<IOpenUrzednikTraceSource>();
        var metrics = Substitute.For<IOpenUrzednikMetrics>();

        // Act
        var telemetry = new ClientTelemetry(logger, traceSource, "OpenUrzednik.Tests.Custom", metrics);

        // Assert
        telemetry.Logger.ShouldBeSameAs(logger);
        telemetry.TraceSource.ShouldBeSameAs(traceSource);
        telemetry.Metrics.ShouldBeSameAs(metrics);
    }
}
