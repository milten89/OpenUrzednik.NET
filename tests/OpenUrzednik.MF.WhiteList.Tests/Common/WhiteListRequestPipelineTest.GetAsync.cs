using System.Net;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListRequestPipelineTest
{
    [Fact]
    public async Task GetAsync_ValidationFails_DoesNotBuildPathOrSendAndRecordsErrors()
    {
        // Arrange
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out var handler);
        var span = CreateRecordingSpan();
        var error = new ValidationError("Invalid.", "rule", "nip", null);
        var pathBuilt = false;

        // Act
        var result = await sut.GetAsync(span, "Operation", OpenUrzednikResult.Failure(error), () =>
        {
            pathBuilt = true;
            return RelativePath;
        }, DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
        pathBuilt.ShouldBeFalse();
        handler.Request.ShouldBeNull();
        // RecordErrors adds one "error" event per error.
        span.Received(1).AddEvent("error", "error.type", ValidationError.ErrorCode);
    }

    [Fact]
    public async Task GetAsync_ValidationFails_LogsValidationFailed()
    {
        // Arrange
        var logger = CreateLogger();
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new OpenUrzednikTelemetry(logger));

        // Act
        await sut.GetAsync(CreateRecordingSpan(), "SearchByNipAsync", OpenUrzednikResult.Failure(new ValidationError("Invalid.", "rule", "nip", null)),
            () => RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}",
            "operation", "SearchByNipAsync");
    }

    [Fact]
    public async Task GetAsync_Success_SendsThePathAndMapsPayload()
    {
        // Arrange
        var sut = CreateSut(Json(HttpStatusCode.OK, """{"message":"m","code":"c"}"""), out var handler);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetAsync(span, "Operation", OpenUrzednikResult.Success(),
            () => RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe("c");
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/1234567890"));
        span.DidNotReceive().AddEvent("error", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_Always_WritesTheDisplayPathToTheHttpSpan()
    {
        // Arrange
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        var httpSpan = CreateRecordingSpan();
        tracer.StartSpan(Arg.Any<string>()).Returns(httpSpan);
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new OpenUrzednikTelemetry(NullOpenUrzednikLogger.Instance, tracer));

        // Act
        await sut.GetAsync(CreateRecordingSpan(), "Operation", OpenUrzednikResult.Success(),
            () => RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        httpSpan.Received(1).SetTag("url.path", "/api/search/nip/{nip}");
        httpSpan.DidNotReceive().SetTag(Arg.Any<string>(), Arg.Is<string>(value => value != null && value.Contains("1234567890")));
    }

    [Fact]
    public async Task GetAsync_RequestFails_RecordsErrorsAndWritesTheDisplayPathToTheMessage()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetAsync(span, "Operation", OpenUrzednikResult.Success(),
            () => RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>();
        error.Message.ShouldBe("Resource at https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/{nip} was not found.");
        span.Received(1).AddEvent("error", "error.type", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetAsync_MapperRejectsPayload_ReturnsSerializationErrorAndLogsMappingFailed()
    {
        // Arrange
        var logger = CreateLogger();
        var sut = CreateSut(Json(HttpStatusCode.OK, "{}"), out _, new OpenUrzednikTelemetry(logger));
        var span = CreateRecordingSpan();
        var exception = new InvalidPayloadException("MF White List API response is missing 'requestId'.");

        // Act
        var result = await sut.GetAsync<Dto.ErrorDto, string?>(span, "Operation", OpenUrzednikResult.Success(),
            () => RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, _ => throw exception, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().Message.ShouldBe("MF White List API response is missing 'requestId'.");
        span.Received(1).SetTag("error.type", SerializationError.ErrorCode);
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, WhiteListEvents.MappingFailed, exception, "Failed to map MF White List response: {reason}",
            "reason", "MF White List API response is missing 'requestId'.");
    }

    [Fact]
    public async Task GetAsync_CallerSynchronizationContext_DoesNotResumeOnIt()
    {
        // Arrange
        var httpClient = new HttpClient(new AsyncResponseHandler(() => Json(HttpStatusCode.OK, """{"code":"c"}""")));
        _httpClients.Add(httpClient);
        var telemetry = new OpenUrzednikTelemetry();
        var sut = new WhiteListRequestPipeline(WhiteListConnection.CreateSearch(httpClient, null, telemetry, new FakeTimeProvider()), telemetry);

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => sut.GetAsync(Substitute.For<IOpenUrzednikSpan>(), "Operation",
            OpenUrzednikResult.Success(), () => RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code,
            TestContext.Current.CancellationToken));

        // Assert
        posts.ShouldBe(0);
    }

    // Completes asynchronously and away from the caller's SynchronizationContext, so the pipeline's awaits really suspend.
    private sealed class AsyncResponseHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return respond();
        }
    }
}
