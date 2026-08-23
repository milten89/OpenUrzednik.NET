using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class NbpExchangeRateTableClientTest
{
    [Fact]
    public async Task GetLatestAsync_StartsSpanWithTableTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        await sut.GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.latest");
        span.Received(1).SetTag("nbp.table", TableType.A);
    }

    [Fact]
    public async Task GetLatestAsync_InvalidTable_LogsDebugAndRecordsErrorsOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, logger: logger, tracer: tracer);

        // Act
        await sut.GetLatestAsync((TableType)99, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetLatestAsync");
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task GetLatestAsync_EmptyArrayResponse_RecordsSingleErrorOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<Dto.ExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, logger: logger, tracer: tracer);

        // Act
        await sut.GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", Arg.Any<string>());
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task GetLatestAsync_HttpRequestFails_RecordsErrorsOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        await sut.GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Rate limited");
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, "Too many requests.");
    }

    [Fact]
    public async Task GetTopCountAsync_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var count = faker.Random.Int(3, 10);
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        await sut.GetTopCountAsync(TableType.A, count, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.top_count");
        span.Received(1).SetTag("nbp.table", TableType.A);
        span.Received(1).SetTag("nbp.top_count", count);
    }

    [Fact]
    public async Task GetTodayAsync_StartsSpanWithTableTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        await sut.GetTodayAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.today");
        span.Received(1).SetTag("nbp.table", TableType.A);
    }

    [Fact]
    public async Task GetAsync_Date_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        await sut.GetAsync(TableType.A, date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.get_date");
        span.Received(1).SetTag("nbp.table", TableType.A);
        span.Received(1).SetTag("nbp.date", date);
    }

    [Fact]
    public async Task GetAsync_DateRange_StartsSpanWithTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        await sut.GetAsync(TableType.A, from, to, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.get_range");
        span.Received(1).SetTag("nbp.table", TableType.A);
        span.Received(1).SetTag("nbp.from", from);
        span.Received(1).SetTag("nbp.to", to);
    }

    [Fact]
    public async Task GetBuySellLatestAsync_StartsSpanWithoutTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, _, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        await sut.GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.buy_sell_latest");
    }

    [Fact]
    public async Task GetBuySellTopCountAsync_StartsSpanWithTopCountTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var count = faker.Random.Int(3, 10);
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        await sut.GetBuySellTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.buy_sell_top_count");
        span.Received(1).SetTag("nbp.top_count", count);
    }

    [Fact]
    public async Task GetBuySellTodayAsync_StartsSpanWithoutTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, _, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        await sut.GetBuySellTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.buy_sell_today");
    }

    [Fact]
    public async Task GetBuySellAsync_Date_StartsSpanWithDateTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterCurrencyMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        await sut.GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.buy_sell_date");
        span.Received(1).SetTag("nbp.date", date);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_StartsSpanWithRangeTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterCurrencyMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        await sut.GetBuySellAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.buy_sell_range");
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
