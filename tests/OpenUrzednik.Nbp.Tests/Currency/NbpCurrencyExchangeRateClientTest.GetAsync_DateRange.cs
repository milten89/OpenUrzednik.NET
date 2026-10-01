using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

using TableType = OpenUrzednik.Nbp.Table.TableType;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class NbpCurrencyExchangeRateClientTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    public async Task GetAsync_DateRange_InvalidCurrency_ReturnsFailureWithoutSendingRequest(string? currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxRatesDateRange));
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetAsync(currency!, from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_DateRange_UndefinedTable_ReturnsFailureWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxRatesDateRange));
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetAsync(currency, from, to, (TableType)42, TestContext.Current.CancellationToken);

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
        var currency = faker.Finance.Currency().Code;
        var to = faker.Date.BeforeCurrencyMinDate();
        var from = to.AddDays(-faker.Random.Int(1, DateRangeValidator.MaxRatesDateRange));
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetAsync(currency, from, to, cancellationToken: TestContext.Current.CancellationToken);

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
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(DateRangeValidator.MaxRatesDateRange + 1);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetAsync(currency, from, to, cancellationToken: TestContext.Current.CancellationToken);

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
        var currency = faker.Finance.Currency().Code;
        var to = faker.Date.BeforeCurrencyMinDate();
        var from = to.AddDays(-DateRangeValidator.MaxRatesDateRange - 1);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetAsync(currency, from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData(TableType.A, NbpTable.A)]
    [InlineData(TableType.B, NbpTable.B)]
    public async Task GetAsync_DateRange_SuccessfulResponse_ReturnsMappedCurrencyExchangeRates(TableType table, NbpTable nbpTable)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxRatesDateRange));
        var dto = new CurrencyExchangeRatesDtoFaker().LinkRandomizerTo(faker).Generate();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(nbpTable, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetAsync(currency, from, to, table, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToCurrencyExchangeRates(dto));
        urlBuilderFactory.Received(1).GetCurrencyBuilder(nbpTable, currency);
        urlBuilder.Received(1).ForDateRange(from, to);
    }

    [Fact]
    public async Task GetAsync_DateRange_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(1);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);


        // Act
        var result = await sut.GetAsync(currency, from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>();
    }

    [Fact]
    public async Task GetAsync_DateRange_RangeOf367Days_SendsRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = new DateOnly(2025, 1, 1);
        var to = from.AddDays(DateRangeValidator.MaxRatesDateRange);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, "USD", urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        await sut.GetAsync("USD", from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull();
    }
}
