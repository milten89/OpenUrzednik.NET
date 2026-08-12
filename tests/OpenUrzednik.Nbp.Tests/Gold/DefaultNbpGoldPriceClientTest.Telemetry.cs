using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Fact]
    public async Task GetAsync_Date_StartsSpanWithDateTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterGoldMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.gold.get_date");
        span.Received(1).SetTag("nbp.date", date);
    }

    [Fact]
    public async Task GetAsync_Date_InvalidDate_LogsDebugAndRecordsErrorsOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, logger: logger, tracer: tracer);

        // Act
        await sut.GetAsync(faker.Date.BeforeGoldMinDate(), TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_Date_EmptyArrayResponse_RecordsSingleErrorOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterGoldMinDate();
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        logger.IsEnabled(OpenUrzednikLogLevel.Debug).Returns(true);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<Dto.GoldPriceDto>()), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, logger: logger, tracer: tracer);

        // Act
        await sut.GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", Arg.Any<string>());
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_Date_HttpRequestFails_RecordsErrorsOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterGoldMinDate();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        span.Received(2).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }

    [Fact]
    public async Task GetLatestAsync_StartsSpanWithoutTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, _, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.gold.latest");
    }

    [Fact]
    public async Task GetTopCountAsync_StartsSpanWithTopCountTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var count = faker.Random.Int(3, 10);
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForTopCount(count).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.gold.top_count");
        span.Received(1).SetTag("nbp.top_count", count);
    }

    [Fact]
    public async Task GetTodayAsync_StartsSpanWithoutTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, _, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.gold.today");
    }

    [Fact]
    public async Task GetAsync_DateRange_StartsSpanWithRangeTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterGoldMinDate();
        var to = from.AddDays(faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.gold.get_range");
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
