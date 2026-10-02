using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.TestCommon;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

// RestProviderProfile.MapErrorAsync: the provider override for non-success responses.
public partial class RestRequestExecutorTest
{
    private static RestRequestExecutor CreateConnectionWithOverride(HttpClient httpClient, Func<ErrorResponseContext, Task<OpenUrzednikError?>> mapErrorAsync, OpenUrzednikTelemetry? telemetry = null)
        => new(httpClient, httpClient.BaseAddress!, new RestProviderProfile("test", "Test API", mapErrorAsync), telemetry: telemetry);

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
}
