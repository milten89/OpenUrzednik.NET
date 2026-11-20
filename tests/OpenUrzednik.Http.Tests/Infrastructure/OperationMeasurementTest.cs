using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public class OperationMeasurementTest
{
    private static readonly object Port = 443;

    private static OperationMeasurement Measure(string operation, string provider, TimeSpan duration, string? errorType, string? serverAddress, object? serverPort)
    {
        var measurement = new OperationMeasurement(operation, provider, serverAddress, serverPort);
        measurement.Complete(duration, errorType);
        return measurement;
    }

    [Fact]
    public void Ctor_NotCompleted_HasNoValueOrErrorTypeYet()
    {
        // Act
        var measurement = new OperationMeasurement("nbp.gold.latest", "nbp", "api.nbp.pl", Port);

        // Assert
        measurement.IsCompleted.ShouldBeFalse();
        measurement.Value.ShouldBe(0);
        measurement.Tags.Select(tag => tag.Key).ShouldBe(["openurzednik.operation", "openurzednik.provider", "server.address", "server.port"]);
    }

    [Fact]
    public void Complete_DurationAndErrorType_SetsValueAndErrorTag()
    {
        // Arrange
        var measurement = new OperationMeasurement("nbp.gold.latest", "nbp", null, null);

        // Act
        measurement.Complete(TimeSpan.FromMilliseconds(250), "not_found");

        // Assert
        measurement.IsCompleted.ShouldBeTrue();
        measurement.Value.ShouldBe(0.25);
        measurement.Count.ShouldBe(3);
        measurement[2].ShouldBe(new KeyValuePair<string, object?>("error.type", "not_found"));
    }

    [Fact]
    public void Ctor_Values_DescribeTheOperationDurationHistogram()
    {
        // Act
        var measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.FromMilliseconds(1250), null, null, null);

        // Assert
        measurement.Name.ShouldBe("openurzednik.client.operation.duration");
        measurement.Name.ShouldBe(OperationMeasurement.InstrumentName);
        measurement.Unit.ShouldBe("s");
        measurement.Description.ShouldBe("Duration of OpenUrzednik.NET client operations.");
        measurement.Value.ShouldBe(1.25);
    }

    [Fact]
    public void BucketBoundaries_AreOpenTelemetrysRpcSetInAscendingOrder()
    {
        // Act
        var boundaries = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, null, null, null).BucketBoundaries;

        // Assert
        boundaries.ShouldBe([0.0001, 0.0005, 0.001, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10, 15, 30, 45, 60]);
    }

    [Fact]
    public void Tags_EveryValue_AllTagsInOrder()
    {
        // Arrange
        var measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, "not_found", "api.nbp.pl", Port);

        // Act
        var tags = measurement.Tags;

        // Assert
        tags.Count.ShouldBe(5);
        tags.ToArray().ShouldBe(
        [
            new("openurzednik.operation", "nbp.gold.latest"),
            new("openurzednik.provider", "nbp"),
            new("error.type", "not_found"),
            new("server.address", "api.nbp.pl"),
            new("server.port", 443),
        ]);
        tags[4].Value.ShouldBeSameAs(Port);
    }

    [Fact]
    public void Tags_Success_NoErrorType()
    {
        // Arrange
        var measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, null, "api.nbp.pl", Port);

        // Act
        var tags = measurement.Tags;

        // Assert
        tags.Count.ShouldBe(4);
        tags.ToArray().ShouldBe(
        [
            new("openurzednik.operation", "nbp.gold.latest"),
            new("openurzednik.provider", "nbp"),
            new("server.address", "api.nbp.pl"),
            new("server.port", 443),
        ]);
    }

    [Theory]
    [InlineData("not_found", null, false, "error.type")]
    [InlineData(null, "api.nbp.pl", false, "server.address")]
    [InlineData(null, null, true, "server.port")]
    public void Tags_OneOptionalValue_ItFollowsTheRequiredTags(string? errorType, string? serverAddress, bool withPort, string expectedKey)
    {
        // Arrange
        var measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, errorType, serverAddress, withPort ? Port : null);

        // Act
        var tags = measurement.Tags;

        // Assert
        tags.Count.ShouldBe(3);
        tags[0].Key.ShouldBe("openurzednik.operation");
        tags[1].Key.ShouldBe("openurzednik.provider");
        tags[2].Key.ShouldBe(expectedKey);
        tags.Select(tag => tag.Key).ShouldBe(["openurzednik.operation", "openurzednik.provider", expectedKey]);
    }

    [Fact]
    public void Tags_NoOptionalValue_OperationAndProviderOnly()
    {
        // Arrange
        var measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, null, null, null);

        // Act
        var tags = measurement.Tags;

        // Assert
        tags.Count.ShouldBe(2);
        tags.ToArray().ShouldBe([new("openurzednik.operation", "nbp.gold.latest"), new("openurzednik.provider", "nbp")]);
    }

    [Fact]
    public void Tags_IsTheMeasurementItself()
    {
        // Arrange
        var measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, null, null, null);

        // Act & Assert: one allocation per recorded operation
        measurement.Tags.ShouldBeSameAs(measurement);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(5, 5)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException(int index, int count)
    {
        // Arrange
        var measurement = count == 5
            ? Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, "not_found", "api.nbp.pl", Port)
            : count == 3
                ? Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, null, "api.nbp.pl", null)
                : Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, null, null, null);

        // Act & Assert
        measurement.Count.ShouldBe(count);
        Should.Throw<ArgumentOutOfRangeException>(() => measurement[index]);
    }

    [Fact]
    public void GetEnumerator_NonGeneric_YieldsTheSameTags()
    {
        // Arrange
        System.Collections.IEnumerable measurement = Measure("nbp.gold.latest", "nbp", TimeSpan.Zero, "not_found", null, null);
        var tags = new List<object>();

        // Act: through the non-generic interface, so its explicit GetEnumerator runs
        foreach (var tag in measurement)
        {
            tags.Add(tag);
        }

        // Assert
        tags.ShouldBe(
        [
            new KeyValuePair<string, object?>("openurzednik.operation", "nbp.gold.latest"),
            new KeyValuePair<string, object?>("openurzednik.provider", "nbp"),
            new KeyValuePair<string, object?>("error.type", "not_found"),
        ]);
    }
}
