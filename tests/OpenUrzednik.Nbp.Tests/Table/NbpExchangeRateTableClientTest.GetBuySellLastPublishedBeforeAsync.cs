using System.Net;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class NbpExchangeRateTableClientTest
{
    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_UnsortedResponse_ReturnsTableWithLatestPublicationDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var tables = new BuySellExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate(3);
        // The latest trading date is on another table than the latest publication date: the publication date decides.
        BuySellExchangeRateTableDto[] dtos =
        [
            new() { TableId = tables[0].TableId, TradingDate = new DateOnly(2026, 10, 3), PublicationDate = new DateOnly(2026, 9, 30), Rates = tables[0].Rates },
            new() { TableId = tables[1].TableId, TradingDate = new DateOnly(2026, 10, 1), PublicationDate = new DateOnly(2026, 10, 2), Rates = tables[1].Rates },
            new() { TableId = tables[2].TableId, TradingDate = new DateOnly(2026, 9, 30), PublicationDate = new DateOnly(2026, 10, 1), Rates = tables[2].Rates },
        ];
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToBuySellExchangeRateTable(dtos[1]));
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_DateWithTimeOfDayAndKind_RequestsFourteenDaysEndingTheDayBefore()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync(new DateTime(2026, 10, 5, 0, 30, 0, DateTimeKind.Local), TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2026, 9, 21), new DateOnly(2026, 10, 4));
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_DayAfterMinDate_SendsRequestEndingOnMinDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2002, 1, 3), TestContext.Current.CancellationToken);

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
        var sut = CreateApiClient(httpClient, NbpTable.C, Substitute.For<INbpUrlBuilder>());

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(year, month, day), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
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
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

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
        var sut = CreateApiClient(httpClient, NbpTable.C, Substitute.For<INbpUrlBuilder>(),
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 3), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>()
            .Message.ShouldBe("'date' should not be later than tomorrow (2026-10-02, Europe/Warsaw).");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_EmptyArrayResponse_ReturnsNotFoundErrorAndRecordsItOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<BuySellExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

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
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder);

        // Act
        var result = await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpServiceUnavailableError>();
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_StartsSpanWithDateTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.C, urlBuilder, tracer: tracer);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.buy_sell_last_published_before");
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
        var sut = CreateApiClient(httpClient, NbpTable.C, Substitute.For<INbpUrlBuilder>(), tracer: tracer);

        // Act
        await sut.GetBuySellLastPublishedBeforeAsync(new DateOnly(2002, 1, 2), TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }
}
