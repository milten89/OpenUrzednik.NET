using System.Net;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class NbpCurrencyExchangeRateClientTest
{
    [Fact]
    public async Task GetLastPublishedBeforeAsync_UnsortedResponse_ReturnsOnlyRateWithLatestPublicationDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var rates = new CurrencyExchangeRateDtoFaker().LinkRandomizerTo(faker).Generate(3);
        var dto = new CurrencyExchangeRatesDto
        {
            CurrencyName = faker.Finance.Currency().Description,
            CurrencyCode = currency,
            Rates =
            [
                new() { TableId = rates[0].TableId, PublicationDate = new DateOnly(2026, 9, 30), Price = rates[0].Price },
                new() { TableId = rates[1].TableId, PublicationDate = new DateOnly(2026, 10, 2), Price = rates[1].Price },
                new() { TableId = rates[2].TableId, PublicationDate = new DateOnly(2026, 10, 1), Price = rates[2].Price },
            ],
        };
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder));

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(currency, new DateOnly(2026, 10, 5), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, currency,
            [new CurrencyRate(rates[1].TableId, new DateTime(2026, 10, 2), rates[1].Price)]));
    }

    [Theory]
    [InlineData(TableType.A, 2026, 9, 21)]
    [InlineData(TableType.B, 2026, 9, 7)]
    public async Task GetLastPublishedBeforeAsync_DateWithTimeOfDayAndKind_RequestsWindowEndingTheDayBefore(TableType table, int fromYear, int fromMonth, int fromDay)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(table == TableType.B ? NbpTable.B : NbpTable.A, "USD", urlBuilder));

        // Act
        await sut.GetLastPublishedBeforeAsync("USD", new DateTime(2026, 10, 5, 23, 59, 59, DateTimeKind.Utc), table, TestContext.Current.CancellationToken);

        // Assert
        // 14 days for table A, 28 for table B (weekly).
        urlBuilder.Received(1).ForDateRange(new DateOnly(fromYear, fromMonth, fromDay), new DateOnly(2026, 10, 4));
    }

    [Fact]
    public void GetLastPublishedBeforeAsync_NullCurrency_ThrowsArgumentNullExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = sut.GetLastPublishedBeforeAsync(null!, new DateOnly(2026, 10, 5), cancellationToken: TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("currency");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void GetLastPublishedBeforeAsync_UndefinedTable_ThrowsArgumentOutOfRangeExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentOutOfRangeException>(() => { _ = sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), (TableType)42, TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("table");
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    public async Task GetLastPublishedBeforeAsync_InvalidCurrency_ReturnsValidationErrorWithoutSendingRequest(string currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(currency, new DateOnly(2026, 10, 5), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>().Metadata["name"].ShouldBe("currency");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_DayAfterMinDate_SendsRequestEndingOnMinDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.A, "USD", urlBuilder));

        // Act
        await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2002, 1, 3), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2001, 12, 20), new DateOnly(2002, 1, 2));
        handler.Request.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(2002, 1, 2)]
    [InlineData(2000, 1, 1)]
    public async Task GetLastPublishedBeforeAsync_DateOnOrBeforeMinDate_ReturnsValidationErrorForDateWithoutSendingRequest(int year, int month, int day)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(year, month, day), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["name"].ShouldBe("date");
        error.Message.ShouldBe("'date' should be later than 2002-01-02.");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_InvalidCurrencyAndDateOnMinDate_ReturnsCombinedValidationErrorsWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetLastPublishedBeforeAsync("US", new DateOnly(2002, 1, 2), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_TomorrowInWarsaw_SendsRequestEndingToday()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.A, "USD", urlBuilder),
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 2), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2026, 9, 18), new DateOnly(2026, 10, 1));
        handler.Request.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_AfterTomorrowInWarsaw_ReturnsValidationErrorWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>(),
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 3), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>()
            .Message.ShouldBe("'date' should not be later than tomorrow (2026-10-02, Europe/Warsaw).");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_EmptyRates_ReturnsNotFoundErrorAndRecordsItOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var dto = new CurrencyExchangeRatesDtoFaker(0).LinkRandomizerTo(faker).Generate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.A, "USD", urlBuilder), tracer: tracer);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.A, "USD", urlBuilder));

        // Act
        var result = await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>().Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_StartsSpanWithCurrencyTableAndDateTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, CreateUrlBuilderFactory(NbpTable.B, "AFN", urlBuilder), tracer: tracer);

        // Act
        await sut.GetLastPublishedBeforeAsync("AFN", new DateOnly(2026, 10, 5), TableType.B, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.last_published_before");
        span.Received(1).SetTag("nbp.currency", "AFN");
        span.Received(1).SetTag("nbp.table", "B");
        span.Received(1).SetTag("nbp.date", "2026-10-05");
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_InvalidDate_RecordsValidationErrorOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out _);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>(), tracer: tracer);

        // Act
        await sut.GetLastPublishedBeforeAsync("USD", new DateOnly(2002, 1, 2), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }
}
