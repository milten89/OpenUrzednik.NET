using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.TestCommon;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestRequestExecutorTest
{
    [Fact]
    public async Task GetAsync_NullRelativePath_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = CreateHttpClient(new Faker().WithConstantSeed(), new HttpResponseMessage(HttpStatusCode.OK));

        // Act && Assert
        (await Should.ThrowAsync<ArgumentNullException>(async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(null!, TypeInfo, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("relativePath");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_EmptyOrWhiteSpaceRelativePath_ThrowsArgumentException(string relativePath)
    {
        // Arrange
        using var httpClient = CreateHttpClient(new Faker().WithConstantSeed(), new HttpResponseMessage(HttpStatusCode.OK));

        // Act && Assert
        (await Should.ThrowAsync<ArgumentException>(async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(relativePath, TypeInfo, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("relativePath");
    }

    [Fact]
    public async Task GetAsync_NullTypeInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK));

        // Act && Assert
        (await Should.ThrowAsync<ArgumentNullException>(async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync<TestDto>(faker.Internet.UrlRootedPath(), null!, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("typeInfo");
    }

    [Fact]
    public async Task GetAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act && Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, cts.Token));
    }

    [Fact]
    public async Task GetAsync_ValidRequest_ReturnDto()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new TestDto(faker.Random.Word(), faker.Random.Int());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out var handler);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull();
        handler.Request!.Headers.Accept.ShouldContain(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetAsync_NotFoundStatusCode_ReturnFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetAsync_TooManyRequestsWithRetryAfterSeconds_ReturnsRateLimitErrorWithDelta()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var retryDelay = TimeSpan.FromSeconds(2);
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryDelay);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBe(retryDelay);
    }

    [Fact]
    public async Task GetAsync_TooManyRequestsWithRetryAfterDateWithResponseDate_ReturnsRateLimitErrorWithDate()
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
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBe(retryDelay);
    }

    [Fact]
    public async Task GetAsync_TooManyRequestsWithRetryAfterDateWithoutResponseDate_ReturnsRateLimitErrorWithDate()
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
        var result = await CreateConnection(httpClient, _telemetryProvider, timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBe(retryDelay);
    }

    [Fact]
    public async Task GetAsync_TooManyRequestsWithoutRetryAfterDate_ReturnsRateLimitErrorWithoutDelay()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

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
    public async Task GetAsync_UnsuccessfulStatusCode_ReturnsMappedErrorWithStatusCode(HttpStatusCode statusCode, string expectedErrorCode)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(statusCode));

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Code.ShouldBe(expectedErrorCode);
        result.Errors[0].Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetAsync_BadRequestWithServerMessage_ReturnsBadRequestErrorWithServerMessage()
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
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith($": {serverMessage}");
    }

    [Fact]
    public async Task GetAsync_BadRequestWithoutBody_ReturnsBadRequestErrorWithoutServerMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var relativePath = faker.Internet.UrlRootedPath();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(relativePath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldStartWith("Test API rejected the request to ");
        error.Message.ShouldEndWith($"{relativePath}.");
    }

    [Fact]
    public async Task GetAsync_BadRequestWithLongBody_TruncatesServerMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var body = new string('x', 2000);
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith($": {new string('x', 500)}");
        error.Message.ShouldNotContain(new string('x', 501));
    }

    [Fact]
    public async Task GetAsync_BadRequestWithUnreadableBody_ReturnsBadRequestErrorAndLogsDebug()
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
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors[0].ShouldBeOfType<BadRequestError>();
        error.Message.ShouldEndWith(".");
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, Arg.Any<Exception>(), "Could not read {provider} error response body.", "provider", "Test API");
    }

    [Fact]
    public async Task GetAsync_SuccessfulResponseWithNullJsonContent_ReturnsFailureWithUnknownError()
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
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetAsync_SuccessfulResponseWithInvalidJson_ReturnsFailureWithSerializationError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json", Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetAsync_HttpClientThrowsHttpRequestException_ReturnsServiceUnavailableErrorAndRecordsException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = new HttpRequestException("Connection refused");
        var handler = new StubHttpMessageHandler(thrown);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Exception.ShouldBeSameAs(thrown);
        error.ToException().InnerException.ShouldBeSameAs(thrown);
        span.Received(1).RecordException(thrown);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, thrown, "{provider} request to {path} failed", "provider", "Test API", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_HttpClientTimeoutElapses_ReturnsRequestTimeoutError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var handler = new DelegatingStubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var timeout = TimeSpan.FromMilliseconds(50);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")), Timeout = timeout };
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
        error.Timeout.ShouldBe(timeout);
        error.Exception.ShouldBeAssignableTo<OperationCanceledException>();
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Timeout");
    }

    [Fact]
    public async Task GetAsync_OptionTimeoutShorterThanHttpClientTimeout_ReturnsRequestTimeoutErrorWithOptionTimeout()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var handler = new DelegatingStubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var timeout = TimeSpan.FromMilliseconds(50);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider, timeout).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(timeout);
    }

    [Theory]
    [InlineData(30_000)]
    [InlineData(-1)] // Timeout.InfiniteTimeSpan
    public async Task GetAsync_OptionTimeoutLongerThanHttpClientTimeout_ReturnsRequestTimeoutErrorWithHttpClientTimeout(double optionMilliseconds)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var handler = new DelegatingStubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var httpClientTimeout = TimeSpan.FromMilliseconds(50);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")), Timeout = httpClientTimeout };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider, TimeSpan.FromMilliseconds(optionMilliseconds))
            .GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(httpClientTimeout);
    }

    [Theory]
    [InlineData("cenyzlota")]
    [InlineData("/cenyzlota")]
    public async Task GetAsync_BaseAddressWithPath_KeepsBasePath(string relativePath)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("gold", 1)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://gateway.example.com/nbp/") };

        // Act
        await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(relativePath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://gateway.example.com/nbp/cenyzlota"));
    }

    [Fact]
    public async Task GetAsync_BodyReadExceedsTimeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NeverEndingStream()) };
        using var httpClient = CreateHttpClient(faker, response);
        httpClient.Timeout = TimeSpan.FromMilliseconds(50);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
    }

    [Fact]
    public async Task GetAsync_CancelledDuringSendAsync_DoesNotRecordExceptionOrLog()
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
        var telemetryProvider = new OpenUrzednikTelemetry(logger, tracer);

        // Act
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, cts.Token));

        // Assert
        span.DidNotReceive().RecordException(Arg.Any<Exception>());
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_CancelledDuringContentRead_DoesNotLogOrRecordAsError()
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
        var telemetryProvider = new OpenUrzednikTelemetry(logger, tracer);

        // Act
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, cts.Token));

        // Assert
        span.DidNotReceive().RecordException(Arg.Any<Exception>());
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_ConnectionFailsDuringRead_ReturnsServiceUnavailableError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = new IOException("Connection reset");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ThrowingContent((_, _) => throw thrown)
        };
        using var httpClient = CreateHttpClient(faker, response);
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        var recorded = error.Exception.ShouldNotBeNull();
        (recorded == thrown || recorded.InnerException == thrown).ShouldBeTrue();
        span.Received(1).RecordException(recorded);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, recorded, "{provider} request to {path} failed", "provider", "Test API", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_ValidRequest_SetsSpanTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new TestDto(faker.Random.Word(), faker.Random.Int());
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        var relativePath = faker.Internet.UrlRootedPath();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(relativePath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("http.request.method", "GET");
        span.Received(1).SetTag("url.path", httpClient.BaseAddress!.AbsolutePath.TrimEnd('/') + relativePath);
        span.Received(1).SetTag("http.response.status_code", (int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAsync_BaseAddressWithPath_SetsAbsoluteUrlPathTag()
    {
        // Arrange
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("a", 1))))
        {
            BaseAddress = new Uri("https://api.example.com/api/"),
        };

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync("/cenyzlota/last/3", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("url.path", "/api/cenyzlota/last/3");
    }

    [Fact]
    public async Task GetAsync_NotFoundStatusCode_LogsDebugWithPath()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "{provider} resource not found: {path}.", "provider", "Test API", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_TooManyRequestsWithDelay_LogsWarningWithDelay()
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
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, null, "{provider} rate limit hit, retry after {delay}.", "provider", "Test API", "delay", retryDelay);
    }

    [Fact]
    public async Task GetAsync_TooManyRequestsWithoutDelay_LogsWarningWithoutDelay()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Warning).Returns(true);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, null, "{provider} rate limit hit.", "provider", "Test API");
    }

    [Fact]
    public async Task GetAsync_TooManyRequests_SetsSpanStatusErrorRateLimited()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

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
    public async Task GetAsync_UnsuccessfulStatusCode_LogsWarningAndSetsSpanStatusError(HttpStatusCode statusCode, string expectedSpanStatus)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(statusCode));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IOpenUrzednikLogger.Log) && Equals(c.GetArguments()[0], OpenUrzednikLogLevel.Warning))
            .ShouldBe(1);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, expectedSpanStatus);
    }

    [Fact]
    public async Task GetAsync_ServerErrorStatusCode_LogsWarningWithStatus()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, logger, _) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.BadGateway));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, null, "{provider} is unavailable, status {status}", "provider", "Test API", "status", (int)HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetAsync_InvalidJson_LogsErrorRecordsExceptionAndSetsSpanStatus()
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
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(relativePath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, Arg.Any<JsonException>(), "Failed to deserialize {provider} response from {path}", "provider", "Test API", "path", relativePath);
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
    public async Task GetAsync_AnyResponse_DisposesResponse(HttpStatusCode statusCode, string body)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var content = new DisposeTrackingContent(body);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(statusCode) { Content = content });

        // Act
        await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        content.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_UnknownCharset_ReturnsSerializationError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var content = new StringContent("{\"name\":\"n\",\"value\":1}");
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=foo");
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAsync_BadRequestBodyReadTimesOut_ReturnsBadRequestErrorWithoutServerMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StreamContent(new NeverEndingStream()) };
        using var httpClient = CreateHttpClient(faker, response);
        httpClient.Timeout = TimeSpan.FromMilliseconds(50);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>().Message.ShouldNotContain(": ");
    }

    [Fact]
    public async Task GetAsync_CallerCancelsDuringBadRequestBodyRead_ThrowsOperationCanceledException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StreamContent(new NeverEndingStream()) };
        using var httpClient = CreateHttpClient(faker, response);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act && Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, cts.Token));
    }

    [Fact]
    public async Task GetAsync_OverrideReturnsError_ReturnsItAndRecordsIt()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity) { Content = new StringContent("""{"code":"WL-115"}""") };
        using var httpClient = CreateHttpClient(faker, response);
        var (telemetry, _, span) = CreateTelemetrySubstitutes();
        span.IsRecording.Returns(true);
        ErrorResponseContext? received = null;
        string? body = null;
        var custom = new BadRequestError("Custom.", 422);

        // Act
        var result = await CreateConnectionWithOverride(httpClient, async context =>
        {
            received = context;
            body = await context.ReadMessageAsync();
            return custom;
        }, telemetry).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(custom);
        received.ShouldNotBeNull().StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        received.RequestPath.ShouldEndWith("path");
        body.ShouldBe("""{"code":"WL-115"}""");
        span.Received(1).SetTag("error.code", BadRequestError.ErrorCode);
    }

    [Fact]
    public async Task GetAsync_OverrideReturnsNull_UsesDefaultMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await CreateConnectionWithOverride(httpClient, _ => Task.FromResult<OpenUrzednikError?>(null))
            .GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetAsync_OverrideOn429_GetsRetryAfter()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
        using var httpClient = CreateHttpClient(faker, response);
        TimeSpan? retryAfter = null;

        // Act
        await CreateConnectionWithOverride(httpClient, context =>
        {
            retryAfter = context.RetryAfter;
            return Task.FromResult<OpenUrzednikError?>(null);
        }).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        retryAfter.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task GetAsync_SuccessfulResponse_DoesNotCallOverride()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, new TestDto("name", 1)));
        var called = false;

        // Act
        var result = await CreateConnectionWithOverride(httpClient, _ =>
        {
            called = true;
            return Task.FromResult<OpenUrzednikError?>(null);
        }).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_UnexpectedException_PropagatesAndMarksSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var handler = new DelegatingStubHttpMessageHandler((_, _) => throw new NotSupportedException("boom"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https").TrimEnd('/') + '/') };
        var (telemetry, _, span) = CreateTelemetrySubstitutes();

        // Act && Assert
        await Should.ThrowAsync<NotSupportedException>(() => CreateConnection(httpClient, telemetry, _timeProvider).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken));
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
        span.Received(1).Dispose();
    }

    [Fact]
    public async Task GetAsync_CallerCancels_DoesNotMarkSpanAsUnexpected()
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
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https").TrimEnd('/') + '/') };
        var (telemetry, _, span) = CreateTelemetrySubstitutes();

        // Act && Assert
        await Should.ThrowAsync<OperationCanceledException>(() => CreateConnection(httpClient, telemetry, _timeProvider).GetAsync("path", TypeInfo, cts.Token));
        span.DidNotReceive().SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
    }

    [Fact]
    public async Task GetAsync_Always_NamesSpanAfterProfile()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, new TestDto("name", 1)));
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(Substitute.For<IOpenUrzednikSpan>());

        // Act
        await CreateConnection(httpClient, new OpenUrzednikTelemetry(traceSource: tracer), _timeProvider).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("test.http.get");
    }

    [Fact]
    public async Task GetAsync_UnexpectedStatus_ReturnsUnknownErrorWithReadableMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.Conflict));

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnknownError>().Message.ShouldBe("Test API returned unexpected status 409.");
    }

    [Fact]
    public async Task GetAsync_OverrideReadsBodyThenReturnsNull_DefaultMappingKeepsServerMessage()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("400 BadRequest - Błędny zakres dat") };
        using var httpClient = CreateHttpClient(faker, response);
        string? first = null;
        string? second = null;

        // Act
        var result = await CreateConnectionWithOverride(httpClient, async context =>
        {
            first = await context.ReadMessageAsync();
            second = await context.ReadMessageAsync();
            return null;
        }).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        first.ShouldBe("400 BadRequest - Błędny zakres dat");
        second.ShouldBe(first);
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>().Message.ShouldEndWith(": 400 BadRequest - Błędny zakres dat");
    }

    [Fact]
    public async Task GetAsync_OverrideThrows_PropagatesAndMarksSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.BadRequest));
        var (telemetry, _, span) = CreateTelemetrySubstitutes();

        // Act && Assert
        await Should.ThrowAsync<JsonException>(() => CreateConnectionWithOverride(httpClient, _ => throw new JsonException("bad"), telemetry)
            .GetAsync("path", TypeInfo, TestContext.Current.CancellationToken));
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
    }
}
