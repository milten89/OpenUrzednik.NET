using System.Net;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

// The not-on-the-list path (ADR-0023): the map's failure keeps the proof, is logged as NotOnList and recorded on the span.
public sealed partial class WhiteListRequestPipelineTest
{
    private static readonly DateTimeOffset Answered = new(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1));

    private static OpenUrzednikResult<string?> NotOnTheList(Dto.ErrorDto _)
        => OpenUrzednikResult.Failure<string?>(new WhiteListNotFoundError("Not on the list.", "rd9zq-99alll1", Answered));

    [Fact]
    public async Task GetOrNotOnListAsync_MapSucceeds_ReturnsTheModelAndLogsNothing()
    {
        // Arrange
        var logger = CreateLogger();
        var sut = CreateSut(Json(HttpStatusCode.OK, """{"message":"m","code":"c"}"""), out _, new ClientTelemetry(logger));
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetOrNotOnListAsync(span, "SearchByNipAsync", OpenUrzednikResult.Success(), () => RelativePath, DisplayPath,
            WhiteListJsonContext.Default.ErrorDto, dto => OpenUrzednikResult.Success(dto.Code), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe("c");
        logger.DidNotReceive().Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        span.DidNotReceive().AddEvent("error", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetOrNotOnListAsync_MapReturnsNotOnList_ReturnsItLogsAndRecordsIt()
    {
        // Arrange
        var logger = CreateLogger();
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new ClientTelemetry(logger));
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetOrNotOnListAsync(span, "SearchByNipAsync", OpenUrzednikResult.Success(), () => RelativePath, DisplayPath,
            WhiteListJsonContext.Default.ErrorDto, NotOnTheList, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.RequestId.ShouldBe("rd9zq-99alll1");
        error.RequestDateTime.ShouldBe(Answered);
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null, "MF White List API has no entity on the list for {operation}",
            "operation", "SearchByNipAsync");
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
        span.Received(1).AddEvent("error", "error.type", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetOrNotOnListAsync_DebugDisabled_DoesNotLogButRecords()
    {
        // Arrange
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new ClientTelemetry(logger));
        var span = CreateRecordingSpan();

        // Act
        await sut.GetOrNotOnListAsync(span, "SearchByNipAsync", OpenUrzednikResult.Success(), () => RelativePath, DisplayPath,
            WhiteListJsonContext.Default.ErrorDto, NotOnTheList, TestContext.Current.CancellationToken);

        // Assert
        logger.Received().IsEnabled(OpenUrzednikLogLevel.Debug);
        logger.DidNotReceive().Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetOrNotOnListAsync_ValidationFails_SendsNothingAndDoesNotMap()
    {
        // Arrange
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out var handler);
        var mapped = false;
        var error = new ValidationError("Invalid.", "rule", "nip", null);

        // Act
        var result = await sut.GetOrNotOnListAsync(CreateRecordingSpan(), "SearchByNipAsync", OpenUrzednikResult.Failure(error), () => RelativePath,
            DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto =>
            {
                mapped = true;
                return OpenUrzednikResult.Success(dto.Code);
            }, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
        handler.Request.ShouldBeNull();
        mapped.ShouldBeFalse();
    }

    [Fact]
    public async Task GetOrNotOnListAsync_RequestFails_ReturnsTheErrorWithoutNotOnList()
    {
        // Arrange
        var logger = CreateLogger();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound), out _, new ClientTelemetry(logger));

        // Act
        var result = await sut.GetOrNotOnListAsync(CreateRecordingSpan(), "SearchByNipAsync", OpenUrzednikResult.Success(), () => RelativePath,
            DisplayPath, WhiteListJsonContext.Default.ErrorDto, NotOnTheList, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<Transport.Http.HttpNotFoundError>();
        logger.DidNotReceive().Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetOrNotOnListAsync_MapperRejectsPayload_ReturnsSerializationError()
    {
        // Arrange
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetOrNotOnListAsync<Dto.ErrorDto, string?>(span, "SearchByNipAsync", OpenUrzednikResult.Success(), () => RelativePath,
            DisplayPath, WhiteListJsonContext.Default.ErrorDto, _ => throw new InvalidPayloadException("MF White List API response is missing 'requestId'."),
            TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        span.Received(1).SetTag("error.type", SerializationError.ErrorCode);
    }

    [Fact]
    public async Task GetOrNotOnListAsync_NotOnList_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new ClientTelemetry(telemetry, telemetry));

        // Act
        var result = await sut.GetOrNotOnListAsync(telemetry.StartSpan("mf_white_list.search.nip"), "SearchByNipAsync", OpenUrzednikResult.Success(),
            () => $"search/nip/{LeakNip}", DisplayPath, WhiteListJsonContext.Default.ErrorDto, NotOnTheList, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, LeakNip);
    }
}
