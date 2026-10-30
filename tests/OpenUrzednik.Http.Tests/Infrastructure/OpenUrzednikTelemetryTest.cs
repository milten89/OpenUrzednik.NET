using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public class OpenUrzednikTelemetryTest
{
    [Fact]
    public void Ctor_NoArguments_UsesNullImplementations()
    {
        // Act
        var telemetry = new OpenUrzednikTelemetry();

        // Assert
        telemetry.Logger.ShouldBeSameAs(NullOpenUrzednikLogger.Instance);
        telemetry.TraceSource.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance);
    }

    [Fact]
    public void Ctor_SourceNameWithoutTraceSource_UsesSharedSourceOnDotNet()
    {
        // Act
        var telemetry = new OpenUrzednikTelemetry(sourceName: "OpenUrzednik.Tests.Fallback");

        // Assert
#if NET
        telemetry.TraceSource.ShouldNotBeSameAs(NullOpenUrzednikTraceSource.Instance);
        telemetry.TraceSource.ShouldBeSameAs(new OpenUrzednikTelemetry(sourceName: "OpenUrzednik.Tests.Fallback").TraceSource);
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

        // Act
        var telemetry = new OpenUrzednikTelemetry(logger, traceSource);

        // Assert
        telemetry.Logger.ShouldBeSameAs(logger);
        telemetry.TraceSource.ShouldBeSameAs(traceSource);
    }
}
