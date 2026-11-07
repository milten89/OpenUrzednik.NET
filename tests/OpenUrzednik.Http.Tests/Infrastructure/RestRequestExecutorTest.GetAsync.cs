using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core;
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
    public async Task GetAsync_AlreadyCancelledToken_SendsNothingAndStartsNoSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(() => CreateConnection(httpClient, new OpenUrzednikTelemetry(traceSource: tracer), _timeProvider)
            .GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, cts.Token));

        // Assert
        handler.Request.ShouldBeNull();
        tracer.DidNotReceive().StartSpan(Arg.Any<string>());
    }

    public static TheoryData<string> AsynchronousSteps => new() { "send", "body", "error-stream", "error-body", "override" };

    [Theory]
    [MemberData(nameof(AsynchronousSteps))]
    public async Task GetAsync_CallerHasSynchronizationContext_NeverResumesOnIt(string asynchronousStep)
    {
        // Arrange
        // Only the given step waits, for a gate opened after GetAsync has returned, so each await on the way to it suspends
        // while the caller's context is current.
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        HttpMessageHandler handler = asynchronousStep switch
        {
            "send" => new GatedHttpMessageHandler(gate.Task, CreateJsonResponse(HttpStatusCode.OK, new TestDto("n", 1))),
            "body" => new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new GatedStream(gate.Task, Encoding.UTF8.GetBytes("""{"name":"n","value":1}""")))
            }),
            "error-stream" => new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new GatedContent(gate.Task, Encoding.UTF8.GetBytes("Invalid date range"))
            }),
            "error-body" => new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StreamContent(new GatedStream(gate.Task, Encoding.UTF8.GetBytes("Invalid date range")))
            }),
            _ => new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)),
        };
        Func<ErrorResponseContext, Task<OpenUrzednikError?>>? mapErrorAsync = asynchronousStep == "override"
            ? _ => gate.Task.ContinueWith(_ => (OpenUrzednikError?)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default)
            : null;
        using var httpClient = new HttpClient(handler);
        var sut = new RestRequestExecutor(httpClient, new Uri("https://api.example.com/api/"), new RestProviderProfile("test", "Test API", mapErrorAsync));
        var context = new CountingSynchronizationContext();
        var callerContext = SynchronizationContext.Current;

        // Act
        Task<OpenUrzednikResult<TestDto>> pending;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            pending = sut.GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(callerContext);
        }
        var completedBeforeGate = pending.IsCompleted;
        gate.SetResult(true);
        var result = await pending;

        // Assert
        completedBeforeGate.ShouldBeFalse();
        result.IsSuccess.ShouldBe(asynchronousStep is "send" or "body");
        context.Posts.ShouldBe(0);
    }

    [Fact]
    public async Task GetAsync_ValidRequest_DoesNotMarkSpanFailed()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, new TestDto("n", 1)));
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        span.DidNotReceive().SetStatus(Arg.Any<OpenUrzednikSpanStatus>(), Arg.Any<string?>());
        span.DidNotReceive().SetTag("error.type", Arg.Any<string>());
        span.Received(1).Dispose();
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
    [InlineData(HttpStatusCode.RequestTimeout, RequestTimeoutError.ErrorCode)]
    [InlineData((HttpStatusCode)429, RateLimitExceededError.ErrorCode)]
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
        result.Errors[0].Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe((int)statusCode);
        result.Errors[0].GetHttpStatusCode().ShouldBe((int)statusCode);
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
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, RestLogEvents.ErrorBodyUnreadable, Arg.Any<Exception>(), "Could not read {provider} error response body.", "provider", "test");
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
    public async Task GetAsync_SuccessfulResponseWithoutContent_ReturnsFailureWithSerializationError()
    {
        // Arrange
        // .NET Framework leaves Content null; .NET sets an empty one.
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetAsync_SuccessfulResponseWithJsonNull_ReturnsSerializationErrorAndMarksSpanFailed()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, MediaTypeNames.Application.Json) };
        using var httpClient = CreateHttpClient(faker, response);
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().Message.ShouldBe("Test API returned an empty response for rates.");
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, RestLogEvents.EmptyResponse, null, "{provider} returned an empty response for {path}", "provider", "test", "path", "rates");
        span.Received(1).SetTag("error.type", SerializationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Empty response");
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
    public async Task GetAsync_HttpClientThrowsHttpRequestException_ReturnsConnectionFailedErrorAndRecordsException()
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
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>();
        error.Exception.ShouldBeSameAs(thrown);
        error.ToException().InnerException.ShouldBeSameAs(thrown);
        span.Received(1).RecordException(thrown);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ConnectionFailed, thrown, "{provider} could not be reached for {path}", "provider", "test", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_HandlerTimesOutWithItsOwnLimit_LogsAndRecordsTimeoutWithoutLimit()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = new TaskCanceledException("The operation didn't complete within the allowed timeout of '00:00:30'.");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(thrown)) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider, TimeSpan.FromSeconds(10)).GetAsync("cenyzlota", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestTimedOut, thrown, "{provider} request to {path} timed out", "provider", "test", "path", "cenyzlota");
        span.Received(1).RecordException(thrown);
        span.Received(1).SetTag("error.type", RequestTimeoutError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Timeout");
    }

    [Fact]
    public async Task GetAsync_BodyReadExceedsTimeout_ReturnsErrorNamingTheTimeout()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var timeout = TimeSpan.FromMilliseconds(100);
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NeverEndingStream()) };
        using var httpClient = CreateHttpClient(faker, response);
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider, timeout).GetAsync("cenyzlota", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
        error.Timeout.ShouldBe(timeout);
        error.Message.ShouldBe("Test API did not respond to cenyzlota within 00:00:00.1000000.");
        var exception = error.Exception.ShouldBeAssignableTo<OperationCanceledException>().ShouldNotBeNull();
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestTimedOut, exception, "{provider} request to {path} timed out after {timeout}",
            "provider", "test", "path", "cenyzlota", "timeout", (TimeSpan?)timeout);
        span.Received(1).RecordException(exception);
        span.Received(1).SetTag("error.type", RequestTimeoutError.ErrorCode);
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

    [Fact]
    public async Task GetAsync_HandlerTimesOutWithItsOwnLimit_ReturnsRequestTimeoutErrorWithoutLimit()
    {
        // Arrange
        // E.g. a resilience handler: its timeout is neither the request deadline nor HttpClient.Timeout.
        var faker = new Faker().WithConstantSeed();
        var thrown = new TaskCanceledException("The operation didn't complete within the allowed timeout of '00:00:30'.");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(thrown)) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider, TimeSpan.FromSeconds(10))
            .GetAsync("cenyzlota", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>();
        error.Timeout.ShouldBeNull();
        error.Message.ShouldBe("Test API request to cenyzlota timed out.");
        error.Exception.ShouldBeSameAs(thrown);
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

#if !NET
    // .NET Framework's response stream checks the token only before a read starts; .NET's honours it (see BodyReadExceedsTimeout).
    [Fact]
    public async Task GetAsync_BodyReadIgnoresTokenAndExceedsTimeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new TokenIgnoringStream()) };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider, TimeSpan.FromMilliseconds(100))
            .GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task GetAsync_BodyReadIgnoresTokenAndCallerCancels_ThrowsOperationCanceledException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new TokenIgnoringStream()) };
        using var httpClient = CreateHttpClient(faker, response);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => CreateConnection(httpClient, _telemetryProvider, _timeProvider)
            .GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, cts.Token));
    }
#endif

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
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), Arg.Any<OpenUrzednikEventId>(), Arg.Any<Exception>(), Arg.Any<string>());
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
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), Arg.Any<OpenUrzednikEventId>(), Arg.Any<Exception>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_ConnectionFailsDuringRead_ReturnsConnectionFailedError()
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
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>();
        var recorded = error.Exception.ShouldNotBeNull();
        (recorded == thrown || recorded.InnerException == thrown).ShouldBeTrue();
        span.Received(1).RecordException(recorded);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ConnectionFailed, recorded, "{provider} could not be reached for {path}", "provider", "test", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_ConnectionFailsWhileBodyStreams_ReturnsConnectionFailedError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = new IOException("Connection reset");
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream(thrown)) };
        using var httpClient = CreateHttpClient(faker, response);
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>();
        error.Exception.ShouldBeSameAs(thrown);
        error.Message.ShouldBe("Test API could not be reached for rates: Connection reset");
        span.Received(1).RecordException(thrown);
        span.Received(1).SetTag("error.type", ConnectionFailedError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ConnectionFailed, thrown, "{provider} could not be reached for {path}", "provider", "test", "path", "rates");
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
        span.Received(1).SetTag("server.address", httpClient.BaseAddress!.Host);
        span.Received(1).SetTag<object>("server.port", 443);
        span.Received(1).SetTag("url.path", httpClient.BaseAddress!.AbsolutePath.TrimEnd('/') + relativePath);
        span.Received(1).SetTag("http.response.status_code", (int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAsync_BaseAddressWithPort_SetsServerAddressAndPortTags()
    {
        // Arrange
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("a", 1))))
        {
            BaseAddress = new Uri("http://gateway.example.com:8080/nbp/"),
        };

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync("/cenyzlota", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("server.address", "gateway.example.com");
        span.Received(1).SetTag<object>("server.port", 8080);
    }

    [Theory]
    [InlineData("http://[::1]:8080/api/", "::1", 8080)]
    [InlineData("https://bücher.example/api/", "xn--bcher-kva.example", 443)]
    public async Task GetAsync_IpV6OrInternationalHost_SetsServerAddressWithoutBracketsInAscii(string baseAddress, string expectedAddress, int expectedPort)
    {
        // Arrange
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("a", 1))))
        {
            BaseAddress = new Uri(baseAddress),
        };

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync("/cenyzlota", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("server.address", expectedAddress);
        span.Received(1).SetTag<object>("server.port", expectedPort);
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
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, RestLogEvents.NotFound, null, "{provider} resource not found: {path}.", "provider", "test", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_NotFoundStatusCode_MarksSpanFailedWithErrorType()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (telemetryProvider, _, span) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Not found");
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
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
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RateLimited, null, "{provider} rate limit hit, retry after {delay}.", "provider", "test", "delay", retryDelay);
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
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RateLimited, null, "{provider} rate limit hit.", "provider", "test");
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
    [InlineData(HttpStatusCode.RequestTimeout, "Timeout")]
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
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ServiceUnavailable, null, "{provider} is unavailable, status {status}", "provider", "test", "status", (int)HttpStatusCode.BadGateway);
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
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, RestLogEvents.DeserializationFailed, Arg.Any<JsonException>(), "Failed to deserialize {provider} response from {path}", "provider", "test", "path", relativePath);
        span.Received(1).RecordException(Arg.Any<JsonException>());
        span.Received(1).SetTag("error.type", SerializationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "{\"name\":\"n\",\"value\":1}")]
    [InlineData(HttpStatusCode.OK, "not-valid-json")]
    [InlineData(HttpStatusCode.OK, "null")]
    [InlineData(HttpStatusCode.NotFound, "")]
    [InlineData((HttpStatusCode)429, "")] // TooManyRequests; an attribute argument must be a constant on .NET Framework
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
    public async Task GetAsync_UnknownCharset_LogsErrorRecordsExceptionAndMarksSpanFailed()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var content = new StringContent("{\"name\":\"n\",\"value\":1}");
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=foo");
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        error.Message.ShouldBe("Error when decoding response from rates");
        var exception = error.Exception.ShouldBeOfType<InvalidOperationException>();
        logger.Received(1).Log(OpenUrzednikLogLevel.Error, RestLogEvents.DecodingFailed, exception, "Failed to decode {provider} response from {path}", "provider", "test", "path", "rates");
        span.Received(1).RecordException(exception);
        span.Received(1).SetTag("error.type", SerializationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
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
    public async Task GetAsync_BadRequestBodyReadTimesOutAfterPartialData_ReturnsBadRequestErrorWithoutServerMessage()
    {
        // Arrange
        // A message cut off by the deadline is dropped, not returned truncated.
        var faker = new Faker().WithConstantSeed();
        var body = new PartialStream(Encoding.UTF8.GetBytes("Invalid"), async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return 0;
        });
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StreamContent(body) };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider, TimeSpan.FromMilliseconds(100))
            .GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var message = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>().Message;
        message.ShouldNotContain("Invalid");
        message.ShouldEndWith(".");
    }

    [Fact]
    public async Task GetAsync_BadRequestBodyReadFailsAfterPartialData_ReturnsBadRequestErrorWithoutServerMessage()
    {
        // Arrange
        // A message cut off by a broken connection is dropped, not returned truncated.
        var faker = new Faker().WithConstantSeed();
        var body = new PartialStream(Encoding.UTF8.GetBytes("Invalid"), _ => Task.FromException<int>(new IOException("Connection reset.")));
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StreamContent(body) };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var message = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>().Message;
        message.ShouldNotContain("Invalid");
        message.ShouldEndWith(".");
    }

    [Fact]
    public async Task GetAsync_BadRequestBodyInUtf16WithByteOrderMark_ReturnsDecodedServerMessage()
    {
        // Arrange
        // The body is read as UTF-8 unless its byte order mark names another encoding.
        var faker = new Faker().WithConstantSeed();
        byte[] body = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Invalid date range")];
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new ByteArrayContent(body) };
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>().Message.ShouldEndWith(": Invalid date range");
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
        span.Received(1).SetTag("error.type", BadRequestError.ErrorCode);
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
    public async Task GetAsync_OverrideOnServiceUnavailableWithRetryAfter_GetsNoRetryAfter()
    {
        // Arrange
        // Only a 429's Retry-After is read: it is the rate limit's delay (ADR-0002).
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
        using var httpClient = CreateHttpClient(faker, response);
        TimeSpan? retryAfter = TimeSpan.MaxValue;

        // Act
        await CreateConnectionWithOverride(httpClient, context =>
        {
            retryAfter = context.RetryAfter;
            return Task.FromResult<OpenUrzednikError?>(null);
        }).GetAsync("path", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        retryAfter.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_UnsuccessfulStatusCode_ReturnsMessageWithAbsoluteRequestUri()
    {
        // Arrange
        // Real handlers set the response's RequestMessage; the executor names its URI rather than the relative path.
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var handler = new DelegatingStubHttpMessageHandler((request, _) =>
        {
            response.RequestMessage = request;
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/api/") };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync("/rates", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>().Message.ShouldBe("Resource at https://api.example.com/api/rates was not found.");
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

    [Fact]
    public async Task GetAsync_HttpClientThrowsResilienceRejection_ReturnsServiceUnavailableError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var thrown = ResilienceRejection.Create("The circuit is now open.", new InvalidOperationException("rejected"));
        var handler = new StubHttpMessageHandler(thrown);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };
        var (telemetryProvider, logger, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        error.Exception.ShouldBeSameAs(thrown);
        error.Metadata.ContainsKey(HttpErrorMetadata.StatusCodeKey).ShouldBeFalse();
        error.GetHttpStatusCode().ShouldBeNull();
        span.Received(1).SetTag("error.type", ServiceUnavailableError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Rejected");
        logger.Received(1).Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestRejected, thrown, "{provider} request to {path} was rejected", "provider", "test", "path", Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_HttpClientWrapsResilienceRejection_ReturnsServiceUnavailableError()
    {
        // Arrange: .NET Framework's HttpClient may wrap what a handler throws.
        var faker = new Faker().WithConstantSeed();
        var thrown = new HttpRequestException("An error occurred while sending the request.",
            ResilienceRejection.Create("The circuit is now open.", new InvalidOperationException("rejected")));
        var handler = new StubHttpMessageHandler(thrown);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(faker.Internet.UrlWithPath("https")) };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(faker.Internet.UrlRootedPath(), TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>().Exception.ShouldBeSameAs(thrown);
    }

    // A fake identifier carried by the request URI, and the display path that hides it (ADR-0003, ADR-0006).
    private const string Identifier = "9876543210";
    private const string IdentifierPath = "search/nip/" + Identifier + "?date=2026-11-06";
    private const string DisplayPath = "search/nip/{nip}?date=2026-11-06";
    private const string DisplayBaseAddress = "https://api.example.com/api/";

    public static TheoryData<string> DisplayPathFailures => new()
    {
        "400", "401", "403", "404", "408", "429", "500", "empty", "malformed", "charset", "body-io", "connection", "timeout",
    };

    [Fact]
    public async Task GetAsync_NullDisplayPath_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK))) { BaseAddress = new Uri(DisplayBaseAddress) };

        // Act && Assert
        (await Should.ThrowAsync<ArgumentNullException>(async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(IdentifierPath, null!, TypeInfo, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("displayPath");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_EmptyOrWhiteSpaceDisplayPath_ThrowsArgumentException(string displayPath)
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK))) { BaseAddress = new Uri(DisplayBaseAddress) };

        // Act && Assert
        (await Should.ThrowAsync<ArgumentException>(async () => await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(IdentifierPath, displayPath, TypeInfo, TestContext.Current.CancellationToken)))
            .ParamName.ShouldBe("displayPath");
    }

    [Fact]
    public async Task GetAsync_DisplayPath_SendsRelativePathAndTagsDisplayPath()
    {
        // Arrange
        var dto = new TestDto("n", 1);
        var handler = new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, dto));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(DisplayBaseAddress) };
        var (telemetry, _, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetry, _timeProvider).GetAsync("/" + IdentifierPath, "/" + DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(dto);
        handler.Request!.RequestUri!.ToString().ShouldBe(DisplayBaseAddress + IdentifierPath);
        span.Received(1).SetTag("url.path", "/api/search/nip/{nip}");
    }

    [Theory]
    [InlineData("search/nip/" + Identifier, "search/nip/{nip}", "/api/search/nip/{nip}")]
    [InlineData("//search/nip/" + Identifier + "?date=2026-11-06", "//search/nip/{nip}?date=2026-11-06", "/api/search/nip/{nip}")]
    [InlineData("?nip=" + Identifier, "?nip={nip}", "/api/")]
    [InlineData("/", "/", "/api/")]
    public async Task GetAsync_DisplayPath_TagsBasePathPlusDisplayPathWithoutQuery(string relativePath, string displayPath, string expectedUrlPath)
    {
        // Arrange
        var handler = new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("n", 1)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(DisplayBaseAddress) };
        var (telemetry, _, span) = CreateTelemetrySubstitutes();

        // Act
        var result = await CreateConnection(httpClient, telemetry, _timeProvider).GetAsync(relativePath, displayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        span.Received(1).SetTag("url.path", expectedUrlPath);
    }

    [Theory]
    [MemberData(nameof(DisplayPathFailures))]
    public async Task GetAsync_DisplayPathAndFailure_IdentifierAppearsNowhere(string failure)
    {
        // Arrange
        using var httpClient = new HttpClient(CreateFailingHandler(failure)) { BaseAddress = new Uri(DisplayBaseAddress) };
        var (telemetry, logger, span) = CreateTelemetrySubstitutes();
        logger.IsEnabled(Arg.Any<OpenUrzednikLogLevel>()).Returns(true);

        // Act
        var result = await CreateConnection(httpClient, telemetry, _timeProvider, TimeSpan.FromSeconds(10))
            .GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem();
        var written = new List<string?> { error.Message, error.ToException().Message };
        written.AddRange(error.Metadata.Values.Select(value => value?.ToString()));
        written.AddRange(logger.ReceivedCalls().Concat(span.ReceivedCalls()).SelectMany(call => call.GetArguments()).Select(argument => argument?.ToString()));
        logger.ReceivedCalls().ShouldNotBeEmpty();
        written.ShouldAllBe(text => text == null || !text.Contains(Identifier));
    }

    [Fact]
    public async Task GetAsync_DisplayPathAndNotFound_ReturnsMessageWithDisplayUri()
    {
        // Arrange
        using var httpClient = new HttpClient(CreateFailingHandler("404")) { BaseAddress = new Uri(DisplayBaseAddress) };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>().Message.ShouldBe($"Resource at {DisplayBaseAddress}{DisplayPath} was not found.");
    }

    [Fact]
    public async Task GetAsync_DisplayPathAndBadRequest_ReturnsBadRequestErrorWithoutServerMessage()
    {
        // Arrange
        using var httpClient = new HttpClient(CreateFailingHandler("400")) { BaseAddress = new Uri(DisplayBaseAddress) };

        // Act
        var result = await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<BadRequestError>().Message.ShouldBe($"Test API rejected the request to {DisplayBaseAddress}{DisplayPath}.");
    }

    [Fact]
    public async Task GetAsync_DisplayPathAndErrorOverride_PassesDisplayUriAndServerMessage()
    {
        // Arrange
        using var httpClient = new HttpClient(CreateFailingHandler("400")) { BaseAddress = new Uri(DisplayBaseAddress) };
        string? requestPath = null;
        string? serverMessage = null;
        var connection = CreateConnectionWithOverride(httpClient, async context =>
        {
            requestPath = context.RequestPath;
            serverMessage = await context.ReadMessageAsync();
            return null;
        });

        // Act
        await connection.GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe(DisplayBaseAddress + DisplayPath);
        serverMessage.ShouldBe("Invalid NIP " + Identifier);
    }

    [Fact]
    public async Task GetAsync_DisplayPath_AttachesDisplayUriToRequest()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("n", 1)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(DisplayBaseAddress) };

        // Act
        await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        DisplayUriRequestOption.TryGet(handler.Request!, out var displayUri).ShouldBeTrue();
        displayUri.ShouldBe(DisplayBaseAddress + DisplayPath);
    }

    [Fact]
    public async Task GetAsync_NoDisplayPath_AttachesNoDisplayUri()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(CreateJsonResponse(HttpStatusCode.OK, new TestDto("n", 1)));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(DisplayBaseAddress) };

        // Act
        await CreateConnection(httpClient, _telemetryProvider, _timeProvider).GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        DisplayUriRequestOption.TryGet(handler.Request!, out _).ShouldBeFalse();
    }

    private static HttpMessageHandler CreateFailingHandler(string failure) => failure switch
    {
        "connection" => new StubHttpMessageHandler(new HttpRequestException("Connection refused")),
        "timeout" => new StubHttpMessageHandler(new TaskCanceledException("The operation was canceled.")),
        _ => new StubHttpMessageHandler(CreateFailingResponse(failure)),
    };

    private static HttpResponseMessage CreateFailingResponse(string failure)
    {
        switch (failure)
        {
            case "400":
                // The server echoes the identifier, as some APIs do.
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid NIP " + Identifier) };
            case "429":
                var rateLimited = new HttpResponseMessage((HttpStatusCode)429);
                rateLimited.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
                return rateLimited;
            case "empty":
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, MediaTypeNames.Application.Json) };
            case "malformed":
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"name\":", Encoding.UTF8, MediaTypeNames.Application.Json) };
            case "charset":
                var content = new StringContent("{\"name\":\"n\",\"value\":1}");
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=foo");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            case "body-io":
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ThrowingContent((_, _) => throw new IOException("Connection reset.")) };
            default:
                return new HttpResponseMessage((HttpStatusCode)int.Parse(failure, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
