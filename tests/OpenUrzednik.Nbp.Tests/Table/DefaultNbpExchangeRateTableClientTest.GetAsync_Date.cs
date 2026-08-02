using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Extensions;
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
    public async Task GetAsync_Date_InvalidTable_ReturnsFailureWithoutSendingRequest(MidTableType table)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(table, date, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Theory]
    [InlineData(2002, 1, 1)]
    [InlineData(2000, 1, 1)]
    public async Task GetAsync_Date_DateBeforeMinDate_ReturnsFailureWithoutSendingRequest(int year, int month, int day)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, new DateOnly(year, month, day), TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetAsync_Date_InvalidTableAndDateBeforeMinDate_ReturnsCombinedValidationErrorsWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.BeforeCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync((MidTableType)99, date, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }
 
    [Fact]
    public async Task GetAsync_Date_SuccessfulResponse_ReturnsMappedExchangeRateTable()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new ExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, [dto]), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, date, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToExchangeRateTable(dto));
    }
 
    [Fact]
    public async Task GetAsync_Date_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, date, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
 
    [Fact]
    public async Task GetAsync_Date_EmptyArrayResponse_ReturnFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<ExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);
 
        // Act
        var result = await sut.GetAsync(MidTableType.A, date, TestContext.Current.CancellationToken);
 
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}