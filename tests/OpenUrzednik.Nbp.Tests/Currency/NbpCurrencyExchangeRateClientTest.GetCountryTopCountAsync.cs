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
    public async Task GetCountryTopCountAsync_InvalidCurrency_ReturnsFailureWithoutSendingRequest(string? currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        int count = faker.Random.Int(3, 10);
        Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetCountryTopCountAsync(currency!, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCountryTopCountAsync_InvalidTopCount_ReturnsFailureWithoutSendingRequest(int topCount)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetCountryTopCountAsync(currency, topCount, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetCountryTopCountAsync_InvalidCurrencyAndTopCount_ReturnsCombinedValidationErrorsWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetCountryTopCountAsync("US", 0, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetCountryTopCountAsync_SuccessfulResponse_ReturnsMappedCountryExchangeRates()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        int count = faker.Random.Int(3, 10);
        var dto = new CountryExchangeRatesDtoFaker(count).LinkRandomizerTo(faker).Generate();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetCountryTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToCountryExchangeRates(dto));
        urlBuilderFactory.Received(1).GetCurrencyBuilder(NbpTable.B, currency);
        urlBuilder.Received(1).ForTopCount(count);
    }

    [Fact]
    public async Task GetCountryTopCountAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        int count = faker.Random.Int(3, 10);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetCountryTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>();
    }
}
