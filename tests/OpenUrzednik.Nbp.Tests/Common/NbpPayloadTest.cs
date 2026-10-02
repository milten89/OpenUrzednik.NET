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
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, Arg.Any<InvalidPayloadException>(), "Failed to map NBP response: {reason}", "reason", error.Message);
    }
}
