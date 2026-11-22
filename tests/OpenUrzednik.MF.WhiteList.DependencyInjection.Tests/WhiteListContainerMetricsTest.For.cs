using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.Diagnostics;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// ADR-0025: a container's clients record on its IMeterFactory meter when it has one, else on the shared meter. On .NET
// AddHttpClient always registers a factory, so the fallback is tested here directly, on every target.
public class WhiteListContainerMetricsTest
{
    [Fact]
    public void For_MeterFactory_RecordsOnItsMeter()
    {
        // Arrange
        using var factory = new TestMeterFactory();

        // Act
        var metrics = WhiteListServiceCollectionExtensions.WhiteListContainerMetrics.For(factory);

        // Assert
        var meter = metrics.ShouldBeOfType<MeterMetrics>().Meter;
        meter.Scope.ShouldBeSameAs(factory);
        meter.Name.ShouldBe(WhiteListTelemetry.SourceName);
    }

    [Fact]
    public void For_NoMeterFactory_ReturnsTheSharedMetrics()
    {
        // Act
        var metrics = WhiteListServiceCollectionExtensions.WhiteListContainerMetrics.For(null);

        // Assert
        metrics.ShouldBeSameAs(MeterMetrics.GetShared(WhiteListTelemetry.SourceName));
    }
}
