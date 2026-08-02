using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTopCountAsync_InvalidTopCount_ReturnsFailureWithoutSendingRequest(int topCount)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker,  new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetTopCountAsync(topCount, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
    
    [Fact]
    public async Task GetTopCountAsync_SuccessfulResponse_ReturnsAllMappedGoldPricesInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var dtos = new GoldPriceDtoFaker().LinkRandomizerTo(faker).Generate(count).ToArray();
        using var httpClient = CreateHttpClient(faker,  CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dtos.Select(x => new GoldPrice(x.Date, x.Price)));
    }
    
    [Fact]
    public async Task GetTopCountAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker,  new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>();
    }
    
    [Fact]
    public async Task GetTopCountAsync_EmptyArrayResponse_ReturnsSuccessWithEmptyArray()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker,  CreateJsonResponse(HttpStatusCode.OK, []), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);
        
        // Act
        var result = await sut.GetTopCountAsync(count, TestContext.Current.CancellationToken);
        
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}