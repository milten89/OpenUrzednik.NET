using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public class NbpPayloadTest
{
    [Fact]
    public void Map_MapperSucceeds_ReturnsMappedValue()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        var telemetryProvider = new OpenUrzednikTelemetry(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance);

        // Act
        var result = NbpPayload.Map(21, x => x * 2, telemetryProvider, span);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        span.DidNotReceiveWithAnyArgs().SetStatus(default, default);
    }

    [Fact]
    public void Map_MapperRejectsPayload_ReturnsSerializationErrorAndRecordsIt()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var telemetryProvider = new OpenUrzednikTelemetry(logger, NullOpenUrzednikTraceSource.Instance);

        // Act
        var result = NbpPayload.Map<object?, int>(null, dto =>
        {
            NbpPayload.EnsurePresent(dto, "rates");
            return 1;
        }, telemetryProvider, span);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        error.Message.ShouldBe("NBP API response is missing 'rates'.");
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, error.Message);
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, NbpEvents.MappingFailed, Arg.Any<InvalidPayloadException>(), "Failed to map NBP response: {reason}", "reason", error.Message);
    }

    [Fact]
    public void Latest_UnsortedItems_ReturnsItemWithLatestDate()
    {
        // Arrange
        string[] items = ["2026-09-30", "2026-10-02", "2026-10-01"];

        // Act
        var result = NbpPayload.Latest(items, static x => DateOnly.FromDateTime(DateTime.Parse(x, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        result.ShouldBeSameAs(items[1]);
    }

    [Fact]
    public void Latest_EqualDates_ReturnsFirstOfThem()
    {
        // Arrange
        var first = new object();
        var second = new object();

        // Act
        var result = NbpPayload.Latest([first, second], static _ => new DateOnly(2026, 10, 2));

        // Assert
        result.ShouldBeSameAs(first);
    }

    [Fact]
    public void Latest_NoItems_ReturnsNull()
    {
        // Act
        var result = NbpPayload.Latest(Array.Empty<object>(), static _ => new DateOnly(2026, 10, 2));

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Latest_NullItem_ThrowsInvalidPayloadException()
    {
        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => NbpPayload.Latest([new object(), null!], static _ => new DateOnly(2026, 10, 2)))
            .Message.ShouldBe("NBP API response is missing 'item'.");
    }
}
