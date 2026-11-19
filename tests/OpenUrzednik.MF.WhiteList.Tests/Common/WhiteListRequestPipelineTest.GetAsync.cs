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

    // ADR-0003: a NIP in the request path, and echoed in the API's error message, reaches no log, span or error. The NIP and the
    // account number are fake, with valid check digits.
    private const string LeakNip = "7368125416";
    private const string LeakAccount = "61109010140000071219812874";

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, """{"code":"WL-113","message":"Nieprawidłowy NIP: 7368125416."}""", typeof(WhiteListBadRequestError), "******5416")]
    [InlineData(HttpStatusCode.BadRequest, """{"code":"WL-113","message":"Nieprawidłowy NIP: 736-812-54-16."}""", typeof(WhiteListBadRequestError), "***-***-54-16")]
    [InlineData(HttpStatusCode.BadRequest, """{"code":"WL-113","message":"Nieprawidłowy NIP: 736 812 54 16."}""", typeof(WhiteListBadRequestError), "*** *** 54 16")]
    [InlineData(HttpStatusCode.NotFound, "", typeof(HttpNotFoundError), null)]
    [InlineData(HttpStatusCode.InternalServerError, """{"code":"WL-100","message":"Błąd dla 7368125416."}""", typeof(WhiteListServiceUnavailableError), "******5416")]
    [InlineData(HttpStatusCode.OK, "{", typeof(SerializationError), null)]
    public async Task GetAsync_NipInPathAndEchoedInTheError_LeaksNothing(HttpStatusCode status, string body, Type errorType, string? masked)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json(status, body), out _, new OpenUrzednikTelemetry(telemetry, telemetry));

        // Act
        var result = await sut.GetAsync(telemetry.StartSpan("mf_white_list.search.by_nip"), "SearchByNipAsync", OpenUrzednikResult.Success(),
            () => $"search/nip/{LeakNip}", DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem();
        error.ShouldBeOfType(errorType);
        if (masked is not null)
            error.Message.ShouldContain(masked);
        result.ShouldNotLeak(telemetry, LeakNip);
    }

    [Fact]
    public async Task GetAsync_EchoInABodyLongerThanTheReadLimit_LeaksNothingAndEchoesNothing()
    {
        // Arrange: the executor reads the first 500 characters, so the JSON is cut and the server's text is dropped, identifiers before
        // and after the cut included.
        var telemetry = new RecordingTelemetry();
        var body = $$"""{"code":"WL-113","message":"Nieprawidłowy NIP: {{LeakNip}}. {{new string('x', 600)}} {{LeakNip}}"}""";
        var sut = CreateSut(Json(HttpStatusCode.BadRequest, body), out _, new OpenUrzednikTelemetry(telemetry, telemetry));

        // Act
        var result = await sut.GetAsync(telemetry.StartSpan("mf_white_list.search.by_nip"), "SearchByNipAsync", OpenUrzednikResult.Success(),
            () => $"search/nip/{LeakNip}", DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.Message.ShouldBe("MF White List API rejected the request to https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/{nip}.");
        error.WhiteListCode.ShouldBeNull();
        result.ShouldNotLeak(telemetry, LeakNip);
    }

    [Fact]
    public async Task GetAsync_AccountInPathAndEchoedGrouped_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var body = """{"code":"WL-109","message":"Nieprawidłowy numer rachunku: 61 1090 1014 0000 0712 1981 2874."}""";
        var sut = CreateSut(Json(HttpStatusCode.BadRequest, body), out _, new OpenUrzednikTelemetry(telemetry, telemetry));

        // Act
        var result = await sut.GetAsync(telemetry.StartSpan("mf_white_list.search.by_bank_account"), "SearchByBankAccountAsync",
            OpenUrzednikResult.Success(), () => $"search/bank-account/{LeakAccount}", "search/bank-account/{bankAccount}",
            WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>().Message.ShouldContain("** **** **** **** **** **** 2874");
        result.ShouldNotLeak(telemetry, LeakAccount);
    }

    [Fact]
    public async Task GetAsync_SuccessWithTheNipInThePath_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json(HttpStatusCode.OK, """{"code":"WL-0","message":"ok"}"""), out _, new OpenUrzednikTelemetry(telemetry, telemetry));

        // Act
        var result = await sut.GetAsync(telemetry.StartSpan("mf_white_list.search.by_nip"), "SearchByNipAsync", OpenUrzednikResult.Success(),
            () => $"search/nip/{LeakNip}", DisplayPath, WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, LeakNip);
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
