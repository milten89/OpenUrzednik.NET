using System.Net;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class NbpCurrencyExchangeRateClientTest
{
    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_UnsortedResponse_ReturnsOnlyRateWithLatestPublicationDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var rates = new BuySellCurrencyExchangeRateDtoFaker().LinkRandomizerTo(faker).Generate(3);
        var dto = new BuySellCurrencyExchangeRatesDto
        {
            CurrencyName = faker.Finance.Currency().Description,
            CurrencyCode = currency,
            Rates =
            [
                new() { TableId = rates[0].TableId, PublicationDate = new DateOnly(2026, 10, 2), Bid = rates[0].Bid, Ask = rates[0].Ask },
                new() { TableId = rates[1].TableId, PublicationDate = new DateOnly(2026, 9, 30), Bid = rates[1].Bid, Ask = rates[1].Ask },
                new() { TableId = rates[2].TableId, PublicationDate = new DateOnly(2026, 10, 1), Bid = rates[2].Bid, Ask = rates[2].Ask },
            ],
        };
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder));

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(currency, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, currency,
            [new CurrencyBuySellRate(rates[0].TableId, new DateTime(2026, 10, 2), rates[0].Ask, rates[0].Bid)]));
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_DateWithTimeOfDayAndKind_RequestsFourteenDaysOfTableCEndingTheDayBefore()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, "USD", urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateTime(2026, 10, 5, 0, 30, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert
        urlBuilderFactory.Received(1).GetCurrencyBuilder(NbpTable.C, "USD");
        urlBuilder.Received(1).ForDateRange(new DateOnly(2026, 9, 21), new DateOnly(2026, 10, 4));
    }

    [Fact]
    public void GetBuySellLastPublishedBeforeAsync_NullCurrency_ThrowsArgumentNullExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = sut.GetBuySellLastPublishedBeforeAsync(null!, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("currency");
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    public async Task GetBuySellLastPublishedBeforeAsync_InvalidCurrency_ReturnsValidationErrorWithoutSendingRequest(string currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(currency, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>().Metadata["name"].ShouldBe("currency");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_DayAfterMinDate_SendsRequestEndingOnMinDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.C, "USD", urlBuilder));

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2002, 1, 3), TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2001, 12, 20), new DateOnly(2002, 1, 2));
        handler.Request.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(2002, 1, 2)]
    [InlineData(2000, 1, 1)]
    public async Task GetBuySellLastPublishedBeforeAsync_DateOnOrBeforeMinDate_ReturnsValidationErrorForDateWithoutSendingRequest(int year, int month, int day)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(year, month, day), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Metadata["name"].ShouldBe("date");
        error.Message.ShouldBe("'date' should be later than 2002-01-02.");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_TomorrowInWarsaw_SendsRequestEndingToday()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.C, "USD", urlBuilder),
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2026, 9, 18), new DateOnly(2026, 10, 1));
        handler.Request.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_AfterTomorrowInWarsaw_ReturnsValidationErrorWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>(),
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 3), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>()
            .Message.ShouldBe("'date' should not be later than tomorrow (2026-10-02, Europe/Warsaw).");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_EmptyRates_ReturnsNotFoundErrorAndRecordsItOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var dto = new BuySellCurrencyExchangeRatesDtoFaker(0).LinkRandomizerTo(faker).Generate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.C, "USD", urlBuilder), tracer: tracer);

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.C, "USD", urlBuilder));

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpRateLimitExceededError>();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_StartsSpanWithCurrencyAndDateTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.C, "USD", urlBuilder), tracer: tracer);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.buy_sell_last_published_before");
        span.Received(1).SetTag("nbp.currency", "USD");
        span.Received(1).SetTag("nbp.date", "2026-10-05");
        span.DidNotReceive().SetTag("nbp.table", Arg.Any<string>());
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_InvalidDate_RecordsValidationErrorOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out _);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>(), tracer: tracer);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2002, 1, 2), TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }
}
