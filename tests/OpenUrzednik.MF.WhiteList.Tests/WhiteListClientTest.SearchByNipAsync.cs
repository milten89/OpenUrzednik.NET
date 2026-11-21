using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    [Fact]
    public void SearchByNipAsync_NullNip_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByNipAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("nip");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByNipAsync_FormattedNip_SendsTheDigitsAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out var handler);

        // Act
        await sut.SearchByNipAsync("PL 736-812-54-16", Date.AddHours(23), TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/7368125416?date=2026-11-02"));
    }

    [Fact]
    public async Task SearchByNipAsync_Success_MapsTheSubjectAndTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out _);

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectSearchResult(TestHostSubject, "EftiD-99alll1", new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1))));
        result.Value.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task SearchByNipAsync_NotOnTheList_ReturnsNotFoundWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip-not-found.json"), out _);

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.RequestId.ShouldBe("rd9zq-99alll1");
        error.RequestDateTime.ShouldBe(new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1)));
        error.Message.ShouldBe("MF White List API has no subject on the list for the identifier on the requested date.");
    }

    [Theory]
    [InlineData("7368125417")] // a wrong check digit
    [InlineData("736812541")] // too short
    [InlineData("DE7368125416")] // another country's prefix
    public async Task SearchByNipAsync_InvalidNip_ReturnsMaskedValidationErrorAndSendsNothing(string nip)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out var handler);

        // Act
        var result = await sut.SearchByNipAsync(nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("nip");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask(nip));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByNipAsync_InvalidNipAndDate_ReturnsBothErrors()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out var handler);

        // Act
        var result = await sut.SearchByNipAsync("7368125417", Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.GetType()).ShouldBe([typeof(ValidationError<string>), typeof(ValidationError<DateTime>)]);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesOutOfRange))]
    public async Task SearchByNipAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out var handler);

        // Act
        var result = await sut.SearchByNipAsync(Nip, date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(date.Date);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesInRange))]
    public async Task SearchByNipAsync_DateAtTheBounds_SendsTheRequest(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-nip.json"), out var handler);

        // Act
        var result = await sut.SearchByNipAsync(Nip, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBe($"?date={date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task SearchByNipAsync_DailyLimit_ReturnsRateLimitErrorWithTheCode()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        // No Retry-After: the time to the next midnight in Warsaw, 12 hours after Now.
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Message.ShouldContain("search/nip/{nip}");
    }

    [Theory]
    [MemberData(nameof(MetricCases), "search-nip.json", "search-nip-not-found.json")]
    public async Task SearchByNipAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.search.nip");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Fact]
    public async Task SearchByNipAsync_InvalidDate_RecordsTheValidationErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture("search-nip.json"), out _, metrics: metrics);

        // Act
        await sut.SearchByNipAsync(Nip, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(ValidationError.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(LeakAnswers), "search-nip.json", "search-nip-not-found.json")]
    public async Task SearchByNipAsync_AnyAnswer_LeaksNoNip(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", Nip)), out _, telemetry);

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, Nip);
    }

    [Fact]
    public async Task SearchByNipAsync_InvalidNip_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-nip.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByNipAsync("7368125417", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, "7368125417");
    }
}
