using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class DefaultNbpCurrencyExchangeRateClientTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    public async Task GetBuySellLatestAsync_InvalidCurrency_ReturnsFailureWithoutSendingRequest(string? currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetBuySellLatestAsync(currency!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuySellLatestAsync_SuccessfulResponse_ReturnsMappedBuySellExchangeRates()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var dto = new BuySellCurrencyExchangeRatesDtoFaker().LinkRandomizerTo(faker).Generate();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetBuySellLatestAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToBuySellExchangeRates(dto));
        urlBuilderFactory.Received(1).GetCurrencyBuilder(NbpTable.C, currency);
        urlBuilder.Received(1).Latest();
    }

    [Fact]
    public async Task GetBuySellLatestAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetBuySellLatestAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}
