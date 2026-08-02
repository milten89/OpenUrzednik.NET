using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class DefaultNbpExchangeRateTableClientTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBuySellTopCountAsync_InvalidTopCount_ReturnsFailureWithoutSendingRequest(int topCount)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);
 
        // Act
        var result = await sut.GetBuySellTopCountAsync(topCount, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetBuySellTopCountAsync_SuccessfulResponse_ReturnsAllMappedBuySellExchangeRateTablesInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var dtos = new BuySellExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate(count).ToArray();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);
 
        // Act
        var result = await sut.GetBuySellTopCountAsync(count, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToBuySellExchangeRateTable(dtos));
    }
 
    [Fact]
    public async Task GetBuySellTopCountAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);
 
        // Act
        var result = await sut.GetBuySellTopCountAsync(count, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>();
    }
 
    [Fact]
    public async Task GetBuySellTopCountAsync_EmptyArrayResponse_ReturnsSuccessWithEmptyArray()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<BuySellExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);
 
        // Act
        var result = await sut.GetBuySellTopCountAsync(count, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}