using System.Net;

using Bogus;

using Microsoft.Extensions.Time.Testing;

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
    public void GetAsync_DateRange_NullCurrency_ThrowsArgumentNullExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxRatesDateRange));
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = sut.GetAsync(null!, from, to, cancellationToken: TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("currency");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void GetAsync_DateRange_UndefinedTable_ThrowsArgumentOutOfRangeExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxRatesDateRange));
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentOutOfRangeException>(() => { _ = sut.GetAsync(currency, from, to, (TableType)42, TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("table");
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
    [InlineData(TableType.A, (int)NbpTable.A)]
    [InlineData(TableType.B, (int)NbpTable.B)]
    public async Task GetAsync_DateRange_SuccessfulResponse_ReturnsMappedCurrencyExchangeRates(TableType table, int nbpTableValue)
    {
        // Arrange
        var nbpTable = (NbpTable)nbpTableValue;
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

    [Fact]
    public async Task GetAsync_DateRange_ToAfterTodayInWarsaw_ReturnsFailureWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>(), timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await sut.GetAsync("USD", new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 2), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is ValidationError && e.Message.Contains("later than today (2026-10-01"));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_DateRangeWithTimeOfDayAndKind_UsesOnlyTheDateParts()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.A, "USD", urlBuilder));

        // Act
        await sut.GetAsync("USD", new DateTime(2026, 10, 1, 0, 30, 0, DateTimeKind.Local), new DateTime(2026, 10, 2, 23, 59, 59, DateTimeKind.Utc), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2));
    }
}
