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

public partial class NbpCurrencyExchangeRateClientTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    public async Task GetTodayAsync_InvalidCurrency_ReturnsFailureWithoutSendingRequest(string? currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetTodayAsync(currency!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetTodayAsync_SuccessfulResponse_ReturnsMappedCurrencyExchangeRates()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var dto = new CurrencyExchangeRatesDtoFaker().LinkRandomizerTo(faker).Generate();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetTodayAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToCurrencyExchangeRates(dto));
        urlBuilderFactory.Received(1).GetCurrencyBuilder(NbpTable.A, currency);
        urlBuilder.Received(1).Today();
    }

    [Fact]
    public async Task GetTodayAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetTodayAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}
