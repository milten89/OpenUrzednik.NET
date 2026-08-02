using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class DefaultNbpExchangeRateTableClientTest
{
    [Theory]
    [InlineData((MidTableType)99)]
    [InlineData((MidTableType)(-1))]
    public async Task GetAsync_DateRange_InvalidTable_ReturnsFailureWithoutSendingRequest(MidTableType table)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(table, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetAsync_DateRange_ToDateBeforeMinDate_ReturnsFailureWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var to = faker.Date.BeforeCurrencyMinDate();
        var from = to.AddDays(-faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetAsync_DateRange_ExceedsMaxDays_ReturnsFailureWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(DateRangeValidator.MaxDateRange + 1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetAsync_DateRange_ToDateBeforeMinDateAndRangeExceedsMaxDays_ReturnsCombinedValidationErrorsWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.BeforeCurrencyMinDate();
        var to = from.AddDays(DateRangeValidator.MaxDateRange + 1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetAsync_DateRange_SuccessfulResponse_ReturnsAllMappedExchangeRateTablesInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var count = faker.Random.Int(3, 10);
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(count);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var dtos = new ExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate(count).ToArray();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToExchangeRateTable(dtos));
    }
 
    [Fact]
    public async Task GetAsync_DateRange_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>();
    }
 
    [Fact]
    public async Task GetAsync_DateRange_EmptyArrayResponse_ReturnsSuccessWithEmptyArray()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<ExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, from, to, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}