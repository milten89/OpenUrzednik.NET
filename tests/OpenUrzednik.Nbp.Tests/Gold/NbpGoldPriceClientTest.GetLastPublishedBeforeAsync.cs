using System.Net;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class NbpGoldPriceClientTest
{
    [Fact]
    public async Task GetLastPublishedBeforeAsync_UnsortedResponse_ReturnsPriceWithLatestDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = new DateOnly(2026, 10, 5);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        GoldPriceDto[] dtos =
        [
            new() { Date = new DateOnly(2026, 9, 30), Price = faker.Finance.Amount(1, 10) },
            new() { Date = new DateOnly(2026, 10, 2), Price = faker.Finance.Amount(1, 10) },
            new() { Date = new DateOnly(2026, 10, 1), Price = faker.Finance.Amount(1, 10) },
        ];
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(dtos[1].Date.ToPublicDate(), dtos[1].Price));
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_DateWithTimeOfDayAndKind_RequestsFourteenDaysEndingTheDayBefore()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        await sut.GetLastPublishedBeforeAsync(new DateTime(2026, 10, 5, 23, 59, 59, DateTimeKind.Utc), TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2026, 9, 21), new DateOnly(2026, 10, 4));
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_DayAfterMinDate_SendsRequestEndingOnMinDate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        await sut.GetLastPublishedBeforeAsync(new DateOnly(2013, 1, 3), TestContext.Current.CancellationToken);

        // Assert
        urlBuilder.Received(1).ForDateRange(new DateOnly(2012, 12, 20), new DateOnly(2013, 1, 2));
        handler.Request.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(2013, 1, 2)]
    [InlineData(2000, 1, 1)]
    public async Task GetLastPublishedBeforeAsync_DateOnOrBeforeMinDate_ReturnsValidationErrorForDateWithoutSendingRequest(int year, int month, int day)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = new DateOnly(year, month, day);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["name"].ShouldBe("date");
        error.Metadata["value"].ShouldBe(date);
        error.Message.ShouldBe("'date' should be later than 2013-01-02.");
        handler.Request.ShouldBeNull();
        urlBuilder.DidNotReceiveWithAnyArgs().ForDateRange(default, default);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_TomorrowInWarsaw_SendsRequestEndingToday()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        await sut.GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

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
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilder>(), timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 3), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Message.ShouldBe("'date' should not be later than tomorrow (2026-10-02, Europe/Warsaw).");
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
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, []), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata.ContainsKey(HttpErrorMetadata.StatusCodeKey).ShouldBeFalse();
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
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>();
        error.Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(404);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_StartsSpanWithDateTag()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder, tracer: tracer);

        // Act
        await sut.GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        tracer.Received(1).StartSpan("nbp.gold.last_published_before");
        span.Received(1).SetTag("nbp.date", "2026-10-05");
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_InvalidDate_RecordsValidationErrorOnSpan()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out _);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilder>(), tracer: tracer);

        // Act
        await sut.GetLastPublishedBeforeAsync(faker.Date.BeforeGoldMinDate(), TestContext.Current.CancellationToken);

        // Assert
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        span.Received(1).SetStatus(OpenUrzednikSpanStatus.Error, Arg.Any<string>());
    }
}
