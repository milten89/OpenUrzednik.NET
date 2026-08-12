using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class DefaultNbpCurrencyExchangeRateClientTest
{
    [Fact]
    public async Task GetTopCountAsync_SuccessfulResponse_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var count = faker.Random.Int(3, 10);
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, logger: logger, tracer: tracer);

        // Act
        await sut.GetTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.top_count");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.top_count", count);
    }

    [Fact]
    public async Task GetTopCountAsync_InvalidCurrencyAndTopCount_LogsDebugAndRecordsErrorsOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out _);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>(), logger: logger, tracer: tracer);

        // Act
        await sut.GetTopCountAsync("US", 0, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTopCountAsync");
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task GetTopCountAsync_HttpRequestFails_RecordsErrorsOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var count = faker.Random.Int(3, 10);
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, logger: logger, tracer: tracer);

        // Act
        await sut.GetTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Rate limited");
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Too many requests.");
    }

    [Fact]
    public async Task GetTopCountAsync_SuccessfulResponse_DoesNotSetErrorStatus()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var count = faker.Random.Int(3, 10);
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, logger: logger, tracer: tracer);

        // Act
        await sut.GetTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        span.DidNotReceive().RecordException(Arg.Any<Exception>());
    }

    [Fact]
    public async Task GetLatestAsync_StartsSpanWithCurrencyTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetLatestAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.latest");
        span.Received(1).SetTag("nbp.currency", currency);
    }

    [Fact]
    public async Task GetTodayAsync_StartsSpanWithCurrencyTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetTodayAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.today");
        span.Received(1).SetTag("nbp.currency", currency);
    }

    [Fact]
    public async Task GetAsync_Date_StartsSpanWithCurrencyAndDateTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var date = faker.Date.AfterCurrencyMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetAsync(currency, date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.get_date");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.date", date);
    }

    [Fact]
    public async Task GetAsync_DateRange_StartsSpanWithCurrencyAndRangeTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetAsync(currency, from, to, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.get_range");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.from", from);
        span.Received(1).SetTag("nbp.to", to);
    }

    [Fact]
    public async Task GetCountryLatestAsync_StartsSpanWithCurrencyTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetCountryLatestAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.country_latest");
        span.Received(1).SetTag("nbp.currency", currency);
    }

    [Fact]
    public async Task GetCountryTopCountAsync_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var count = faker.Random.Int(3, 10);
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetCountryTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.country_top_count");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.top_count", count);
    }

    [Fact]
    public async Task GetCountryTodayAsync_StartsSpanWithCurrencyTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetCountryTodayAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.country_today");
        span.Received(1).SetTag("nbp.currency", currency);
    }

    [Fact]
    public async Task GetCountryAsync_Date_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var date = faker.Date.AfterCurrencyMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetCountryAsync(currency, date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.country_date");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.date", date);
    }

    [Fact]
    public async Task GetCountryAsync_DateRange_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.B, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetCountryAsync(currency, from, to, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.country_range");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.from", from);
        span.Received(1).SetTag("nbp.to", to);
    }

    [Fact]
    public async Task GetBuySellLatestAsync_StartsSpanWithCurrencyTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetBuySellLatestAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.buy_sell_latest");
        span.Received(1).SetTag("nbp.currency", currency);
    }

    [Fact]
    public async Task GetBuySellTopCountAsync_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var count = faker.Random.Int(3, 10);
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetBuySellTopCountAsync(currency, count, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.buy_sell_top_count");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.top_count", count);
    }

    [Fact]
    public async Task GetBuySellTodayAsync_StartsSpanWithCurrencyTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetBuySellTodayAsync(currency, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.buy_sell_today");
        span.Received(1).SetTag("nbp.currency", currency);
    }

    [Fact]
    public async Task GetBuySellAsync_Date_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var date = faker.Date.AfterCurrencyMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetBuySellAsync(currency, date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.buy_sell_date");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.date", date);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.C, currency, urlBuilder);
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilderFactory, tracer: tracer);

        // Act
        await sut.GetBuySellAsync(currency, from, to, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.currency.buy_sell_range");
        span.Received(1).SetTag("nbp.currency", currency);
        span.Received(1).SetTag("nbp.from", from);
        span.Received(1).SetTag("nbp.to", to);
    }

    private static (IOpenUrzednikLogger Logger, IOpenUrzednikSpan Span, IOpenUrzednikTraceSource Tracer) CreateTelemetrySubstitutes()
    {
        var logger = Substitute.For<IOpenUrzednikLogger>();
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        return (logger, span, tracer);
    }
}
