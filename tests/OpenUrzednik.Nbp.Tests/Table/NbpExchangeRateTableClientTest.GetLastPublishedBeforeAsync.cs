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
    public async Task GetLastPublishedBeforeAsync_UnsortedResponse_ReturnsTableWithLatestPublicationDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var tables = new ExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate(3);
        ExchangeRateTableDto[] dtos =
        [
            new() { TableId = tables[0].TableId, PublicationDate = new DateOnly(2026, 10, 1), Rates = tables[0].Rates },
            new() { TableId = tables[1].TableId, PublicationDate = new DateOnly(2026, 9, 30), Rates = tables[1].Rates },
            new() { TableId = tables[2].TableId, PublicationDate = new DateOnly(2026, 10, 2), Rates = tables[2].Rates },
        ];
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToExchangeRateTable(dtos[2]));
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
        var sut = CreateApiClient(httpClient, table == TableType.B ? NbpTable.B : NbpTable.A, urlBuilder);

        // Act
        await sut.GetLastPublishedBeforeAsync(table, new DateTime(2026, 10, 5, 23, 59, 59, DateTimeKind.Utc), TestContext.Current.CancellationToken);

        // Assert
        // 14 days for table A, 28 for table B (weekly).
        urlBuilder.Received(1).ForDateRange(new DateOnly(fromYear, fromMonth, fromDay), new DateOnly(2026, 10, 4));
    }

    [Fact]
    public void GetLastPublishedBeforeAsync_UndefinedTable_ThrowsArgumentOutOfRangeExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, Substitute.For<INbpUrlBuilder>());

        // Act && Assert
        Should.Throw<ArgumentOutOfRangeException>(() => { _ = sut.GetLastPublishedBeforeAsync((TableType)42, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("table");
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
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);

        // Act
        await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2002, 1, 3), TestContext.Current.CancellationToken);

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
        var sut = CreateApiClient(httpClient, NbpTable.A, Substitute.For<INbpUrlBuilder>());

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(year, month, day), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["name"].ShouldBe("date");
        error.Message.ShouldBe("'date' should be later than 2002-01-02.");
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
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

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
        var sut = CreateApiClient(httpClient, NbpTable.A, Substitute.For<INbpUrlBuilder>(),
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2026, 10, 3), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>()
            .Message.ShouldBe("'date' should not be later than tomorrow (2026-10-02, Europe/Warsaw).");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_EmptyArrayResponse_ReturnsNotFoundErrorAndRecordsItOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<ExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder, tracer: tracer);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

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
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>().Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_StartsSpanWithTableAndDateTags()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.B, urlBuilder, tracer: tracer);

        // Act
        await sut.GetLastPublishedBeforeAsync(TableType.B, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.table.last_published_before");
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
        var sut = CreateApiClient(httpClient, NbpTable.A, Substitute.For<INbpUrlBuilder>(), tracer: tracer);

        // Act
        await sut.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2002, 1, 2), TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }
}
