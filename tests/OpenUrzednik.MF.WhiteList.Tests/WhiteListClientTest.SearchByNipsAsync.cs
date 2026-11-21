using System.Net;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    // search-nips.json answers four NIPs: a subject, one not on the list, one the API rejected, and the first one again.
    private static readonly string[] FourNips = [Nip, Nip2, Nip, Nip2];

    [Fact]
    public void SearchByNipsAsync_NullList_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByNipsAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("nips");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void SearchByNipsAsync_NullElement_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByNipsAsync([Nip, Nip2, null!], Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("nips");
        exception.Message.ShouldStartWith("'nips[2]' is null.");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByNipsAsync_FormattedNipsWithDuplicates_SendsTheDigitsInOrderAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        await sut.SearchByNipsAsync(["PL 736-812-54-16", Nip2, Nip, "316-997-30-97"], Date.AddHours(23), TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri($"https://wykazpodatnikowvat.mf.gov.pl/api/search/nips/{Nip},{Nip2},{Nip},{Nip2}?date=2026-11-02"));
    }

    [Fact]
    public async Task SearchByNipsAsync_Success_MapsEveryEntryInOrderWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out _);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BatchSearchResult(
        [
            new BatchSearchEntry("3245174504", [TestHostSubject], null),
            NotOnList("5260250274", "r5wrK-99b0ck2"),
            Rejected("1234567890", "Nieprawidłowy NIP.", "WL-115"),
            new BatchSearchEntry("3245174504", [TestHostSubject], null),
        ], "r5wrK-99b0ck2", BatchAnswered));
        result.Value.Entries.Select(e => e.IsSuccess).ShouldBe([true, false, false, true]);
    }

    [Fact]
    public async Task SearchByNipsAsync_FailedEntries_LogsTheirCountsAtDebug()
    {
        // Arrange: the fixture has one entry not on the list and one rejected
        var logger = Substitute.For<IOpenUrzednikLogger>();
        logger.IsEnabled(Arg.Any<OpenUrzednikLogLevel>()).Returns(true);
        var sut = CreateSut(Fixture("search-nips.json"), out _, logger);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null,
            "MF White List API answered {operation} with {notOnList} entries not on the list and {rejected} rejected",
            "operation", "SearchByNipsAsync", "notOnList", 1, "rejected", 1);
    }

    [Fact]
    public async Task SearchByNipsAsync_EntryNotOnTheList_IsNotFoundWithTheBatchProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out _);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Value.Entries[1].Error.ShouldBeOfType<WhiteListNotFoundError>();
        error.ShouldBeAssignableTo<NotFoundError>();
        error.RequestId.ShouldBe(result.Value.RequestId);
        error.RequestDateTime.ShouldBe(result.Value.RequestDateTime);
    }

    [Fact]
    public async Task SearchByNipsAsync_EntryError_IsQueryRejectedWithTheCode()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out _);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        var entry = result.Value.Entries[2];
        entry.Subjects.ShouldBeEmpty();
        var error = entry.Error.ShouldBeOfType<WhiteListQueryRejectedError>();
        error.ShouldBeAssignableTo<BadRequestError>();
        error.WhiteListCode.ShouldBe("WL-115");
        error.Message.ShouldBe("MF White List API rejected the identifier: WL-115 Nieprawidłowy NIP.");
    }

    [Fact]
    public async Task SearchByNipsAsync_AnotherNumberOfEntries_ReturnsSerializationError()
    {
        // Arrange: the answer has four entries, the request three NIPs
        var sut = CreateSut(Fixture("search-nips.json"), out _);

        // Act
        var result = await sut.SearchByNipsAsync([Nip, Nip2, Nip], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>()
            .Message.ShouldBe("MF White List API response has another number of 'entries' than the identifiers sent.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public async Task SearchByNipsAsync_CountAtTheBounds_SendsEveryNip(int count)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        await sut.SearchByNipsAsync(Batch(count, Nip, Nip2), Date, TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri!.AbsolutePath.ShouldBe($"/api/search/nips/{string.Join(",", Batch(count, Nip, Nip2))}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task SearchByNipsAsync_CountOutOfRange_ReturnsCountValidationErrorAndSendsNothing(int count)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        var result = await sut.SearchByNipsAsync(Batch(count, Nip, Nip2), Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<int>>();
        error.Name.ShouldBe("nips");
        error.Value.ShouldBe(count);
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByNipsAsync_InvalidNips_ReturnsAMaskedErrorPerPositionAndSendsNothing()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        var result = await sut.SearchByNipsAsync([Nip, "7368125417", Nip2, "DE7368125416"], Date, TestContext.Current.CancellationToken);

        // Assert
        var errors = result.Errors.Select(e => e.ShouldBeOfType<ValidationError<string>>()).ToList();
        errors.Select(e => e.Name).ShouldBe(["nips[1]", "nips[3]"]);
        errors.Select(e => e.Value).ShouldBe([OpenUrzednik.Privacy.SensitiveValue.Mask("7368125417"), OpenUrzednik.Privacy.SensitiveValue.Mask("DE7368125416")]);
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByNipsAsync_TooManyWithAnInvalidOneAndAnInvalidDate_ReturnsEveryError()
    {
        // Arrange: the 31st NIP is invalid, past the names the client keeps ready
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);
        var nips = Batch(31, Nip, Nip2);
        nips[30] = "7368125417";

        // Act
        var result = await sut.SearchByNipsAsync(nips, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => ((ValidationError)e).Name).ShouldBe(["nips", "nips[30]", "date"]);
        result.Errors.Select(e => e.GetType()).ShouldBe([typeof(ValidationError<int>), typeof(ValidationError<string>), typeof(ValidationError<DateTime>)]);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesOutOfRange))]
    public async Task SearchByNipsAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(date.Date);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesInRange))]
    public async Task SearchByNipsAsync_DateAtTheBounds_SendsTheRequest(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nips.json"), out var handler);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBe($"?date={date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task SearchByNipsAsync_DailyLimit_ReturnsRateLimitErrorWithTheRouteTemplate()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Message.ShouldContain("search/nips/{nips}");
    }

    [Fact]
    public async Task SearchByNipsAsync_TooManyForTheApi_ReturnsBadRequestWithWl130()
    {
        // Arrange: the API's own limit, should a batch the client accepts ever exceed it
        var sut = CreateSut(Fixture(HttpStatusCode.BadRequest, "error-400-wl-130.json"), out _);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>().WhiteListCode.ShouldBe("WL-130");
    }

    [Theory]
    [MemberData(nameof(BatchMetricCases), "search-nips.json")]
    public async Task SearchByNipsAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.search.nips");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Fact]
    public async Task SearchByNipsAsync_InvalidCount_RecordsTheValidationErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture("search-nips.json"), out _, metrics: metrics);

        // Act
        await sut.SearchByNipsAsync([], Date, TestContext.Current.CancellationToken);

        // Assert
        metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(ValidationError.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(BatchLeakAnswers))]
    public async Task SearchByNipsAsync_AnyAnswer_LeaksNoNip(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", Nip)), out _, telemetry);

        // Act
        var result = await sut.SearchByNipsAsync([Nip], Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, Nip);
        EntryErrors(result).ShouldNotLeak(telemetry, Nip);
    }

    [Fact]
    public async Task SearchByNipsAsync_CapturedAnswer_LeaksNoNip()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-nips.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByNipsAsync(FourNips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        EntryErrors(result).ShouldNotLeak(telemetry, Nip, Nip2);
    }

    [Fact]
    public async Task SearchByNipsAsync_InvalidNips_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-nips.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByNipsAsync([Nip, "7368125417", "PL3169973098"], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Count.ShouldBe(2);
        result.ShouldNotLeak(telemetry, Nip, "7368125417", "3169973098");
    }
}
