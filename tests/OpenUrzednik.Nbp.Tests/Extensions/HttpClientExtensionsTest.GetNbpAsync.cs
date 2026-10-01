using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.TestCommon;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public partial class HttpClientExtensionsTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
    private static readonly JsonTypeInfo<TestDto> TypeInfo = (JsonTypeInfo<TestDto>)JsonOptions.GetTypeInfo(typeof(TestDto));

    private readonly TimeProvider _timeProvider = new FakeTimeProvider();
    private readonly NbpTelemetryProvider _telemetryProvider = new(NullOpenUrzednikLogger.Instance, NullOpenUrzednikTraceSource.Instance);

    [Fact]
    public async Task GetNbpAsync_NullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = null!;

        // Act && Assert
        (await Should.ThrowAsync<ArgumentNullException>(async () => await httpClient.GetNbpAsync("", TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("httpClient");
    }

    [Fact]
    public async Task GetNbpAsync_NullRelativePath_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = CreateHttpClient(new Faker().WithConstantSeed(), new HttpResponseMessage(HttpStatusCode.OK));

        // Act && Assert
        (await Should.ThrowAsync<ArgumentNullException>(async () => await httpClient.GetNbpAsync(null!, TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("relativePath");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetNbpAsync_EmptyOrWhiteSpaceRelativePath_ThrowsArgumentException(string relativePath)
    {
        // Arrange
        using var httpClient = CreateHttpClient(new Faker().WithConstantSeed(), new HttpResponseMessage(HttpStatusCode.OK));

        // Act && Assert
        (await Should.ThrowAsync<ArgumentException>(async () => await httpClient.GetNbpAsync(relativePath, TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("relativePath");
    }

    [Fact]
    public async Task GetNbpAsync_NullTypeInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK));

        // Act && Assert
        (await Should.ThrowAsync<ArgumentNullException>(async () => await httpClient.GetNbpAsync<TestDto>(faker.Internet.UrlRootedPath(), null!, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("typeInfo");
    }

    [Fact]
    public async Task GetNbpAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act && Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, cts.Token));
    }

    [Fact]
    public async Task GetNbpAsync_ValidRequest_ReturnDto()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new TestDto(faker.Random.Word(), faker.Random.Int());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out var handler);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull();
        handler.Request!.Headers.Accept.ShouldContain(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetNbpAsync_NotFoundStatusCode_ReturnFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequestsWithRetryAfterSeconds_ReturnsRateLimitErrorWithDelta()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var retryDelay = TimeSpan.FromSeconds(2);
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryDelay);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBe(retryDelay);
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequestsWithRetryAfterDateWithResponseDate_ReturnsRateLimitErrorWithDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var responseDate = new DateTimeOffset(2026, 10, 21, 7, 0, 0, TimeSpan.Zero);
        var retryDelay = TimeSpan.FromSeconds(2);
        var retryAfterDate = responseDate + retryDelay;
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.Date = responseDate;
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfterDate);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBe(retryDelay);
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequestsWithRetryAfterDateWithoutResponseDate_ReturnsRateLimitErrorWithDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var responseDate = new DateTimeOffset(2026, 10, 21, 7, 0, 0, TimeSpan.Zero);
        var retryDelay = TimeSpan.FromSeconds(2);
        var retryAfterDate = responseDate + retryDelay;
        var timeProvider = new FakeTimeProvider(responseDate);
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfterDate);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBe(retryDelay);
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequestsWithoutRetryAfterDate_ReturnsRateLimitErrorWithoutDelay()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, BadRequestError.ErrorCode)]
    [InlineData(HttpStatusCode.Unauthorized, UnauthorizedError.ErrorCode)]
    [InlineData(HttpStatusCode.Forbidden, UnauthorizedError.ErrorCode)]
    [InlineData(HttpStatusCode.NotFound, NotFoundError.ErrorCode)]
    [InlineData(HttpStatusCode.TooManyRequests, RateLimitExceededError.ErrorCode)]
    [InlineData(HttpStatusCode.InternalServerError, ServiceUnavailableError.ErrorCode)]
    [InlineData(HttpStatusCode.BadGateway, ServiceUnavailableError.ErrorCode)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ServiceUnavailableError.ErrorCode)]
    [InlineData(HttpStatusCode.GatewayTimeout, ServiceUnavailableError.ErrorCode)]
    [InlineData(HttpStatusCode.Conflict, UnknownError.ErrorCode)]
    [InlineData(HttpStatusCode.MultipleChoices, UnknownError.ErrorCode)]
    public async Task GetNbpAsync_UnsuccessfulStatusCode_ReturnsMappedErrorWithStatusCode(HttpStatusCode statusCode, string expectedErrorCode)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(statusCode));

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe(expectedErrorCode);
        result.Errors[0].Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetNbpAsync_BadRequestWithServerMessage_ReturnsBadRequestErrorWithServerMessage()
    {
        // Arrange
        const string serverMessage = "400 BadRequest - Błędny zakres dat / Invalid date range";
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent($"{serverMessage}\r\n", Encoding.UTF8, MediaTypeNames.Text.Plain)
        };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith($": {serverMessage}");
    }

    [Fact]
    public async Task GetNbpAsync_BadRequestWithoutBody_ReturnsBadRequestErrorWithoutServerMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var relativePath = faker.Internet.UrlRootedPath();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        var result = await httpClient.GetNbpAsync(relativePath, TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldStartWith("NBP API rejected the request to ");
        error.Message.ShouldEndWith($"{relativePath}.");
    }

    [Fact]
    public async Task GetNbpAsync_BadRequestWithLongBody_TruncatesServerMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var body = new string('x', 2000);
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith($": {new string('x', 500)}");
        error.Message.ShouldNotContain(new string('x', 501));
    }

    [Fact]
    public async Task GetNbpAsync_BadRequestWithUnreadableBody_ReturnsBadRequestErrorAndLogsDebug()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new ThrowingContent((_, _) => throw new IOException("Connection reset."))
        };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(".");
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, Arg.Any<Exception>(), "Could not read NBP error response body.");
    }

    [Fact]
    public async Task GetNbpAsync_SuccessfulResponseWithNullJsonContent_ReturnsFailureWithUnknownError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetNbpAsync_SuccessfulResponseWithInvalidJson_ReturnsFailureWithSerializationError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json", Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetNbpAsync_HttpClientThrows_RecordsExceptionOnSpanAndRethrows()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = new HttpRequestException("Connection refused");
        var handler = new StubHttpMessageHandler(thrown);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };

        var span = Substitute.For<IOpenUrzednikSpan>();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        var telemetryProvider = new NbpTelemetryProvider(NullOpenUrzednikLogger.Instance, tracer);

        // Act
        var actual = await Should.ThrowAsync<HttpRequestException>(
            async () => await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken));

        // Assert
        actual.ShouldBeSameAs(thrown);
        span.Received(1).RecordException(thrown);
    }

    [Fact]
    public async Task GetNbpAsync_CancelledDuringSendAsync_DoesNotRecordExceptionOrLog()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var cts = new CancellationTokenSource();
        var handler = new DelegatingStubHttpMessageHandler(async (_, ct) =>
        {
            await cts.CancelAsync();
            ct.ThrowIfCancellationRequested();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };

        var span = Substitute.For<IOpenUrzednikSpan>();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var telemetryProvider = new NbpTelemetryProvider(logger, tracer);

        // Act
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, cts.Token));

        // Assert
        span.DidNotReceive().RecordException(Arg.Any<Exception>());
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetNbpAsync_CancelledDuringContentRead_DoesNotLogOrRecordAsError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var cts = new CancellationTokenSource();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ThrowingContent((_, _) => { cts.Cancel(); throw new OperationCanceledException(); })
        };
        using var httpClient = CreateHttpClient(faker, response);

        var span = Substitute.For<IOpenUrzednikSpan>();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var telemetryProvider = new NbpTelemetryProvider(logger, tracer);

        // Act
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, cts.Token));

        // Assert
        span.DidNotReceive().RecordException(Arg.Any<Exception>());
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetNbpAsync_UnexpectedExceptionDuringRead_LogsRecordsAndRethrows()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = new IOException("Connection reset");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ThrowingContent((_, _) => throw thrown)
        };
        using var httpClient = CreateHttpClient(faker, response);

        var span = Substitute.For<IOpenUrzednikSpan>();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        var logger = Substitute.For<IOpenUrzednikLogger>();
        logger.IsEnabled(Arg.Any<OpenUrzednikLogLevel>()).Returns(true);
        var telemetryProvider = new NbpTelemetryProvider(logger, tracer);

        // Act
        var actual = await Should.ThrowAsync<HttpRequestException>(
            async () => await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken));

        // Assert
        actual.InnerException.ShouldBeSameAs(thrown);
        span.Received(1).RecordException(actual);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, actual, Arg.Any<string>(), "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetNbpAsync_ValidRequest_SetsSpanTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new TestDto(faker.Random.Word(), faker.Random.Int());
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        var relativePath = faker.Internet.UrlRootedPath();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto));

        // Act
        await httpClient.GetNbpAsync(relativePath, TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("http.path", relativePath);
        span.Received(1).SetTag("http.status_code", (int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNbpAsync_NotFoundStatusCode_LogsDebugWithPath()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "NBP resource not found: {path}.", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequestsWithDelay_LogsWarningWithDelay()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var retryDelay = TimeSpan.FromSeconds(5);
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Warning).Returns(true);
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryDelay);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, null, "NBP rate limit hit, retry after {delay}.", "delay", retryDelay);
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequestsWithoutDelay_LogsWarningWithoutDelay()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Warning).Returns(true);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, null, "NBP rate limit hit.");
    }

    [Fact]
    public async Task GetNbpAsync_TooManyRequests_SetsSpanStatusErrorRateLimited()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Rate limited");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Unauthorized")]
    [InlineData(HttpStatusCode.InternalServerError, "Service unavailable")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service unavailable")]
    [InlineData(HttpStatusCode.Conflict, "Unexpected status")]
    public async Task GetNbpAsync_UnsuccessfulStatusCode_LogsWarningAndSetsSpanStatusError(HttpStatusCode statusCode, string expectedSpanStatus)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(statusCode));

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        logger.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IOpenUrzednikLogger.Log) && Equals(c.GetArguments()[0], OpenUrzednikLogLevel.Warning))
            .ShouldBe(1);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, expectedSpanStatus);
    }

    [Fact]
    public async Task GetNbpAsync_ServerErrorStatusCode_LogsWarningWithStatus()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.BadGateway));

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, null, "NBP API is unavailable, status {status}", "status", (int)HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetNbpAsync_InvalidJson_LogsErrorRecordsExceptionAndSetsSpanStatus()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();
        var relativePath = faker.Internet.UrlRootedPath();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json", Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        await httpClient.GetNbpAsync(relativePath, TypeInfo, telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, Arg.Any<JsonException>(), "Failed to deserialize NBP response from {path}", "path", relativePath);
        span.Received(1).RecordException(Arg.Any<JsonException>());
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "{\"name\":\"n\",\"value\":1}")]
    [InlineData(HttpStatusCode.OK, "not-valid-json")]
    [InlineData(HttpStatusCode.OK, "null")]
    [InlineData(HttpStatusCode.NotFound, "")]
    [InlineData(HttpStatusCode.TooManyRequests, "")]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    public async Task GetNbpAsync_AnyResponse_DisposesResponse(HttpStatusCode statusCode, string body)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var content = new DisposeTrackingContent(body);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(statusCode) { Content = content });

        // Act
        await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _telemetryProvider, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        content.IsDisposed.ShouldBeTrue();
    }

    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response)
        => CreateHttpClient(faker, response, out _);

    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        var baseAddress = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/';
        handler = new StubHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, TestDto dto)
        => new(statusCode) { Content = JsonContent.Create(dto, TypeInfo) };

    private static (NbpTelemetryProvider Provider, IOpenUrzednikLogger Logger, IOpenUrzednikSpan Span) CreateTelemetrySubstitutes()
    {
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var span = Substitute.For<IOpenUrzednikSpan>();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        return (new NbpTelemetryProvider(logger, tracer), logger, span);
    }

    private sealed record TestDto(string Name, int Value);

    private sealed class DisposeTrackingContent(string body) : StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
