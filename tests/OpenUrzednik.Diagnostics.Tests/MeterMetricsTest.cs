using System.Diagnostics.Metrics;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public class MeterMetricsTest
{
    private static string UniqueName() => $"OpenUrzednik.Tests.{Guid.NewGuid():N}";

    [Fact]
    public void GetShared_SameName_ReturnsSameInstance()
    {
        // Arrange
        var name = UniqueName();

        // Act
        var first = MeterMetrics.GetShared(name);
        var second = MeterMetrics.GetShared(name);

        // Assert
        second.ShouldBeSameAs(first);
        first.Meter.Name.ShouldBe(name);
    }

    [Fact]
    public void GetShared_DifferentNames_ReturnsDifferentMeters()
    {
        // Act
        var first = MeterMetrics.GetShared(UniqueName());
        var second = MeterMetrics.GetShared(UniqueName());

        // Assert
        second.Meter.ShouldNotBeSameAs(first.Meter);
    }

#if NET
    [Fact]
    public void GetShared_SameNameAsTheClientsDefault_SharesItsMeter()
    {
        // Arrange
        var name = UniqueName();

        // Act
        var metrics = MeterMetrics.GetShared(name);

        // Assert: the clients' .NET default (Core's internal shared meter) and the adapter record on one meter
        metrics.Meter.ShouldBeSameAs(SharedOpenUrzednikMeter.Get(name).Meter);
    }
#endif

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetShared_EmptyName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => MeterMetrics.GetShared(name));
    }

    [Fact]
    public void GetShared_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => MeterMetrics.GetShared(null!));
    }

    [Fact]
    public void Record_HistogramMeasurement_CreatesTheInstrumentAndRecordsValueAndTags()
    {
        // Arrange
        var name = UniqueName();
        using var recorder = new MeterRecorder(name);
        var metrics = MeterMetrics.GetShared(name);
        var measurement = new TestHistogramMeasurement(0.25, ("openurzednik.operation", "nbp.gold.latest"), ("error.type", "not_found"));

        // Act
        metrics.Record(measurement);

        // Assert
        var recorded = recorder.Measurements.ShouldHaveSingleItem();
        recorded.Value.ShouldBe(0.25);
        recorded.Tags.ShouldBe([new("openurzednik.operation", "nbp.gold.latest"), new("error.type", "not_found")]);
        var instrument = recorded.Instrument.ShouldBeOfType<Histogram<double>>();
        instrument.Name.ShouldBe(TestHistogramMeasurement.InstrumentName);
        instrument.Unit.ShouldBe("s");
        instrument.Description.ShouldBe("A test histogram.");
    }

    [Fact]
    public void Record_SameNameTwice_RecordsOnOneInstrument()
    {
        // Arrange
        var name = UniqueName();
        using var recorder = new MeterRecorder(name);
        var metrics = MeterMetrics.GetShared(name);

        // Act
        metrics.Record(new TestHistogramMeasurement(1));
        metrics.Record(new TestHistogramMeasurement(2));

        // Assert
        recorder.Instruments.ShouldHaveSingleItem();
        recorder.Measurements.Select(m => m.Value).ShouldBe([1d, 2d]);
        recorder.Measurements[1].Instrument.ShouldBeSameAs(recorder.Measurements[0].Instrument);
    }

    [Fact]
    public void Record_KindItDoesNotKnow_IsIgnored()
    {
        // Arrange
        var name = UniqueName();
        using var recorder = new MeterRecorder(name);
        var metrics = MeterMetrics.GetShared(name);

        // Act
        metrics.Record(new UnknownKindMeasurement());

        // Assert
        recorder.Instruments.ShouldBeEmpty();
        metrics.IsEnabled(UnknownKindMeasurement.InstrumentName).ShouldBeTrue("no instrument was created for it");
    }

#if NET9_0_OR_GREATER || NETFRAMEWORK
    [Fact]
    public void Record_HistogramWithBucketBoundaries_CreatesTheInstrumentWithThemAsAdvice()
    {
        // Arrange
        var name = UniqueName();
        using var recorder = new MeterRecorder(name);
        var metrics = MeterMetrics.GetShared(name);

        // Act
        metrics.Record(new TestHistogramMeasurement(1) { BucketBoundaries = [0.1, 1, 10] });

        // Assert
        var instrument = recorder.Instruments.ShouldHaveSingleItem().ShouldBeOfType<Histogram<double>>();
        instrument.Advice.ShouldNotBeNull().HistogramBucketBoundaries.ShouldBe([0.1, 1, 10]);
    }
#endif

    [Fact]
    public void Record_HistogramWithoutBucketBoundaries_CreatesTheInstrumentWithoutAdvice()
    {
        // Arrange
        var name = UniqueName();
        using var recorder = new MeterRecorder(name);
        var metrics = MeterMetrics.GetShared(name);

        // Act
        metrics.Record(new TestHistogramMeasurement(1));

        // Assert
        var instrument = recorder.Instruments.ShouldHaveSingleItem().ShouldBeOfType<Histogram<double>>();
#if NET9_0_OR_GREATER || NETFRAMEWORK
        instrument.Advice.ShouldBeNull();
#else
        instrument.ShouldNotBeNull();
#endif
    }

    [Fact]
    public void IsEnabled_BeforeTheInstrumentExists_ReturnsTrue()
    {
        // Arrange
        var metrics = MeterMetrics.GetShared(UniqueName());

        // Act
        var enabled = metrics.IsEnabled(TestHistogramMeasurement.InstrumentName);

        // Assert
        enabled.ShouldBeTrue();
    }

    [Fact]
    public void IsEnabled_InstrumentWithoutListener_ReturnsFalse()
    {
        // Arrange
        var metrics = MeterMetrics.GetShared(UniqueName());
        metrics.Record(new TestHistogramMeasurement(1));

        // Act
        var enabled = metrics.IsEnabled(TestHistogramMeasurement.InstrumentName);

        // Assert
        enabled.ShouldBeFalse();
    }

    [Fact]
    public void IsEnabled_InstrumentWithListener_ReturnsTrue()
    {
        // Arrange
        var name = UniqueName();
        using var recorder = new MeterRecorder(name);
        var metrics = MeterMetrics.GetShared(name);
        metrics.Record(new TestHistogramMeasurement(1));

        // Act
        var enabled = metrics.IsEnabled(TestHistogramMeasurement.InstrumentName);

        // Assert
        enabled.ShouldBeTrue();
    }

    [Fact]
    public void Record_InstrumentWithoutListener_DoesNotReadTheTags()
    {
        // Arrange
        var metrics = MeterMetrics.GetShared(UniqueName());
        metrics.Record(new TestHistogramMeasurement(1));
        var measurement = new TestHistogramMeasurement(2) { ThrowOnTags = true };

        // Act & Assert
        Should.NotThrow(() => metrics.Record(measurement));
    }

    private sealed class TestHistogramMeasurement(double value, params (string Key, object? Value)[] tags) : IOpenUrzednikHistogramMeasurement
    {
        public const string InstrumentName = "openurzednik.test.duration";

        private readonly KeyValuePair<string, object?>[] _tags = tags.Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value)).ToArray();

        public string Name => InstrumentName;

        public string Unit => "s";

        public string Description => "A test histogram.";

        public bool ThrowOnTags { get; init; }

        public IReadOnlyList<KeyValuePair<string, object?>> Tags => ThrowOnTags ? throw new InvalidOperationException() : _tags;

        public double Value { get; } = value;

        public IReadOnlyList<double>? BucketBoundaries { get; init; }
    }

    private sealed class UnknownKindMeasurement : IOpenUrzednikMeasurement
    {
        public const string InstrumentName = "openurzednik.test.unknown";

        public string Name => InstrumentName;

        public string Unit => "{item}";

        public string Description => "A kind MeterMetrics doesn't know.";

        public IReadOnlyList<KeyValuePair<string, object?>> Tags => [];
    }
}
