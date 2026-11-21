using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    [Fact]
    public void SearchByRegonAsync_NullRegon_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByRegonAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("regon");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByRegonAsync_FormattedRegon_SendsTheDigitsAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out var handler);

        // Act
        await sut.SearchByRegonAsync("593-728-162", Date.AddHours(23), TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/search/regon/593728162?date=2026-11-02"));
    }

    [Fact]
    public async Task SearchByRegonAsync_Success_MapsTheSubjectAndTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out _);

        // Act
        var result = await sut.SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectSearchResult(MinistryOfFinance, "I854A-99allna", new DateTimeOffset(2026, 11, 20, 22, 45, 22, TimeSpan.FromHours(1))));
        result.Value.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task SearchByRegonAsync_NotOnTheList_ReturnsNotFoundWithTheProof()
    {
        // Arrange
        // The answer without a subject has the same shape for a REGON as for a NIP.
        var sut = CreateSut(Fixture("search-nip-not-found.json"), out _);

        // Act
        var result = await sut.SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.RequestId.ShouldBe("rd9zq-99alll1");
        error.RequestDateTime.ShouldBe(new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1)));
        error.Message.ShouldBe("MF White List API has no subject on the list for the identifier on the requested date.");
    }

    [Theory]
    [InlineData("593728163")] // a wrong check digit
    [InlineData("59372816")] // too short
    [InlineData("59372816A")] // a letter
    public async Task SearchByRegonAsync_InvalidRegon_ReturnsMaskedValidationErrorAndSendsNothing(string regon)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonAsync(regon, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("regon");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask(regon));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByRegonAsync_InvalidRegonAndDate_ReturnsBothErrors()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonAsync("593728163", Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.GetType()).ShouldBe([typeof(ValidationError<string>), typeof(ValidationError<DateTime>)]);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesOutOfRange))]
    public async Task SearchByRegonAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonAsync(Regon, date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(date.Date);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesInRange))]
    public async Task SearchByRegonAsync_DateAtTheBounds_SendsTheRequest(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regon.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonAsync(Regon, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBe($"?date={date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task SearchByRegonAsync_DailyLimit_ReturnsRateLimitErrorWithTheCode()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        // No Retry-After: the time to the next midnight in Warsaw, 12 hours after Now.
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Message.ShouldContain("search/regon/{regon}");
    }

    [Theory]
    [MemberData(nameof(MetricCases), "search-regon.json", "search-nip-not-found.json")]
    public async Task SearchByRegonAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.search.regon");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Fact]
    public async Task SearchByRegonAsync_InvalidDate_RecordsTheValidationErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture("search-regon.json"), out _, metrics: metrics);

        // Act
        await sut.SearchByRegonAsync(Regon, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(ValidationError.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(LeakAnswers), "search-regon.json", "search-nip-not-found.json")]
    public async Task SearchByRegonAsync_AnyAnswer_LeaksNoRegon(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", Regon)), out _, telemetry);

        // Act
        var result = await sut.SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, Regon);
    }

    [Fact]
    public async Task SearchByRegonAsync_InvalidRegon_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-regon.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByRegonAsync("593728163", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, "593728163");
    }
}
