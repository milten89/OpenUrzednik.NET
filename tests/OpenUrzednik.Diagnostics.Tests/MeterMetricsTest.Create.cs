using System.Diagnostics.Metrics;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0025: the DI packages record on their container's IMeterFactory meter when it has one.
public partial class MeterMetricsTest
{
    [Fact]
    public void Create_Measurement_RecordsOnTheFactorysMeter()
    {
        // Arrange
        using var factory = new TestMeterFactory();
        var name = UniqueName();
        var values = new List<double>();
        using var listener = ListenToScope(factory, values);

        // Act
        var metrics = MeterMetrics.Create(factory, name);
        metrics.Record(new TestHistogramMeasurement(1.5));

        // Assert: the factory's meter, named like the shared one and, like it, without a version or tags (so OpenTelemetry merges them)
        values.ShouldBe([1.5]);
        metrics.Meter.Scope.ShouldBeSameAs(factory);
        metrics.Meter.Name.ShouldBe(name);
        metrics.Meter.Version.ShouldBeNull();
        metrics.Meter.Tags.ShouldBeNull();
    }

    [Fact]
    public void Create_SameNameAsShared_UsesAnotherMeter()
    {
        // Arrange
        using var factory = new TestMeterFactory();
        var name = UniqueName();

        // Act
        var metrics = MeterMetrics.Create(factory, name);

        // Assert
        metrics.Meter.ShouldNotBeSameAs(MeterMetrics.GetShared(name).Meter);
        metrics.ShouldNotBeSameAs(MeterMetrics.Create(factory, name));
    }

    [Fact]
    public void Record_AfterTheFactoryIsDisposed_RecordsNothingAndDoesNotThrow()
    {
        // Arrange: the instrument exists before the container (the factory) is disposed
        var factory = new TestMeterFactory();
        var values = new List<double>();
        using var listener = ListenToScope(factory, values);
        var metrics = MeterMetrics.Create(factory, UniqueName());
        metrics.Record(new TestHistogramMeasurement(1));

        // Act
        factory.Dispose();
        metrics.Record(new TestHistogramMeasurement(2));

        // Assert
        values.ShouldBe([1]);
        metrics.IsEnabled(TestHistogramMeasurement.InstrumentName).ShouldBeFalse();
    }

    [Fact]
    public void Create_NullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => MeterMetrics.Create(null!, UniqueName())).ParamName.ShouldBe("meterFactory");
    }

    [Fact]
    public void Create_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        using var factory = new TestMeterFactory();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => MeterMetrics.Create(factory, null!)).ParamName.ShouldBe("name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyName_ThrowsArgumentException(string name)
    {
        // Arrange
        using var factory = new TestMeterFactory();

        // Act & Assert
        Should.Throw<ArgumentException>(() => MeterMetrics.Create(factory, name)).ParamName.ShouldBe("name");
    }

    // The values recorded on the meters of one factory.
    private static MeterListener ListenToScope(TestMeterFactory factory, List<double> values)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (ReferenceEquals(instrument.Meter.Scope, factory))
                    l.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<double>((_, value, _, _) =>
        {
            lock (values)
                values.Add(value);
        });
        listener.Start();
        return listener;
    }
}
