using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    // search-regons.json answers three REGONs: a subject, one not on the list and one the API rejected.
    private static readonly string[] ThreeRegons = [Regon, Regon2, Regon];

    // search-regons.json: the test host's fictitious subject for REGON 216973362, its empty bases sent as "".
    private static readonly Subject TestHostSubject13 = new("Nazwa Firmy 13")
    {
        Nip = "1647209764",
        VatStatus = VatStatus.Active,
        Regon = "216973362",
        Krs = "1234567890",
        ResidenceAddress = "TAKA a Taka 799/328, 14-384 Miejscowość 13",
        RegistrationLegalDate = new DateTime(2017, 6, 27),
        HasVirtualAccounts = false,
    };

    [Fact]
    public void SearchByRegonsAsync_NullList_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByRegonsAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("regons");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void SearchByRegonsAsync_NullElement_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByRegonsAsync([null!, Regon], Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("regons");
        exception.Message.ShouldStartWith("'regons[0]' is null.");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByRegonsAsync_FormattedRegons_SendsTheDigitsInOrderAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out var handler);

        // Act
        await sut.SearchByRegonsAsync(["593-728-162", Regon2, " 593 728 162 "], Date, TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri($"https://wykazpodatnikowvat.mf.gov.pl/api/search/regons/{Regon},{Regon2},{Regon}?date=2026-11-02"));
    }

    [Fact]
    public async Task SearchByRegonsAsync_Success_MapsEveryEntryInOrderWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out _);

        // Act
        var result = await sut.SearchByRegonsAsync(ThreeRegons, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BatchSearchResult(
        [
            new BatchSearchEntry("216973362", [TestHostSubject13], null),
            NotOnList("755016841", "K4oSY-99b0ck2"),
            Rejected("123456789", "Nieprawidłowy REGON.", "WL-107"),
        ], "K4oSY-99b0ck2", BatchAnswered));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task SearchByRegonsAsync_CountOutOfRange_ReturnsCountValidationErrorAndSendsNothing(int count)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonsAsync(Batch(count, Regon, Regon2), Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<int>>();
        error.Name.ShouldBe("regons");
        error.Value.ShouldBe(count);
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByRegonsAsync_InvalidRegons_ReturnsAMaskedErrorPerPositionAndSendsNothing()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonsAsync(["593728163", Regon, Regon2], Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("regons[0]");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask("593728163"));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByRegonsAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-regons.json"), out var handler);

        // Act
        var result = await sut.SearchByRegonsAsync(ThreeRegons, MinDate.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>().Name.ShouldBe("date");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByRegonsAsync_DailyLimit_ReturnsRateLimitErrorWithTheRouteTemplate()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.SearchByRegonsAsync(ThreeRegons, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.Message.ShouldContain("search/regons/{regons}");
    }

    [Theory]
    [MemberData(nameof(BatchMetricCases), "search-regons.json")]
    public async Task SearchByRegonsAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.SearchByRegonsAsync(ThreeRegons, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.search.regons");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Theory]
    [MemberData(nameof(BatchLeakAnswers))]
    public async Task SearchByRegonsAsync_AnyAnswer_LeaksNoRegon(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", Regon)), out _, telemetry);

        // Act
        var result = await sut.SearchByRegonsAsync([Regon], Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, Regon);
        EntryErrors(result).ShouldNotLeak(telemetry, Regon);
    }

    [Fact]
    public async Task SearchByRegonsAsync_InvalidRegons_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-regons.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByRegonsAsync([Regon, "593728163"], Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, Regon, "593728163");
    }
}
