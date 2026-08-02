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
    [InlineData((MidTableType)99)]
    [InlineData((MidTableType)(-1))]
    public async Task GetTopCountAsync_InvalidTable_ReturnsFailureWithoutSendingRequest(MidTableType table)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetTopCountAsync(table, count, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTopCountAsync_InvalidTopCount_ReturnsFailureWithoutSendingRequest(int topCount)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetTopCountAsync(MidTableType.A, topCount, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetTopCountAsync_InvalidTableAndTopCount_ReturnsCombinedValidationErrorsWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetTopCountAsync((MidTableType)99, 0, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetTopCountAsync_SuccessfulResponse_ReturnsAllMappedExchangeRateTablesInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var dtos = new ExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate(count).ToArray();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetTopCountAsync(MidTableType.A, count, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToExchangeRateTable(dtos));
    }
 
    [Fact]
    public async Task GetTopCountAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetTopCountAsync(MidTableType.A, count, TestContext.Current.CancellationToken);
 
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
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<ExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetTopCountAsync(MidTableType.A, count, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}