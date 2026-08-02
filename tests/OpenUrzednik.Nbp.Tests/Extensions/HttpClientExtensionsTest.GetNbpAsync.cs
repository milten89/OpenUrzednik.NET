using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Bogus;
using Microsoft.Extensions.Time.Testing;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Extensions;
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

    [Fact]
    public async Task GetNbpAsync_NullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = null!;

        // Act
        var exception = await Record.ExceptionAsync(() => httpClient.GetNbpAsync("", TypeInfo, _timeProvider, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("httpClient");
    }
    
    [Fact]
    public async Task GetNbpAsync_NullRelativePath_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = CreateHttpClient(new Faker().WithConstantSeed(), new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var exception = await Record.ExceptionAsync(() => httpClient.GetNbpAsync(null!, TypeInfo, _timeProvider, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("relativePath");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetNbpAsync_EmptyOrWhiteSpaceRelativePath_ThrowsArgumentException(string relativePath)
    {
        // Arrange
        using var httpClient = CreateHttpClient(new Faker().WithConstantSeed(), new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var exception = await Record.ExceptionAsync(() => httpClient.GetNbpAsync(relativePath, TypeInfo, _timeProvider, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeOfType<ArgumentException>()
            .ParamName.ShouldBe("relativePath");
    }
    
    [Fact]
    public async Task GetNbpAsync_NullTypeInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var exception = await Record.ExceptionAsync(() => httpClient.GetNbpAsync<TestDto>(faker.Internet.UrlRootedPath(), null!, _timeProvider, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
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

        // Act
        var exception = await Record.ExceptionAsync(() => httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, cts.Token));

        // Assert
        exception.ShouldBeOfType<OperationCanceledException>();
    }
    
    [Fact]
    public async Task GetNbpAsync_ValidRequest_ReturnDto()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateTestDto(faker);
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out var handler);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, timeProvider, TestContext.Current.CancellationToken);

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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>()
            .RetryAfter.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetNbpAsync_UnsuccessfulStatusCode_ReturnsFailureWithUnknownError(HttpStatusCode statusCode)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var response = new HttpResponseMessage(statusCode);
        using var httpClient = CreateHttpClient(faker, response);

        // Act
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<UnknownError>();
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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

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
        var result = await httpClient.GetNbpAsync(faker.Internet.UrlRootedPath(), TypeInfo, _timeProvider, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<SerializationError>();
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
    
    private static TestDto CreateTestDto(Faker faker)
        => new(faker.Random.Word(), faker.Random.Int());
    
    private sealed record TestDto(string Name, int Value);
}