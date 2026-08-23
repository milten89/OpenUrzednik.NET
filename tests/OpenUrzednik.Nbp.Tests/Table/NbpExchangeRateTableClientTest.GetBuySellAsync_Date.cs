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

public partial class NbpExchangeRateTableClientTest
{
    [Theory]
    [InlineData(2002, 1, 1)]
    [InlineData(2000, 1, 1)]
    public async Task GetBuySellAsync_Date_DateBeforeMinDate_ReturnsFailureWithoutSendingRequest(int year, int month, int day)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        var result = await sut.GetBuySellAsync(new DateOnly(year, month, day), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuySellAsync_Date_SuccessfulResponse_ReturnsMappedBuySellExchangeRateTable()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new BuySellExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, [dto]), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        var result = await sut.GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToBuySellExchangeRateTable(dto));
    }

    [Fact]
    public async Task GetBuySellAsync_Date_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        var result = await sut.GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetBuySellAsync_Date_EmptyArrayResponse_ReturnFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<BuySellExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        var result = await sut.GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}
