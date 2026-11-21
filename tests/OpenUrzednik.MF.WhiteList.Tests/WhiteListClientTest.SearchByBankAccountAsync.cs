using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    [Fact]
    public void SearchByBankAccountAsync_NullBankAccount_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByBankAccountAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("bankAccount");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByBankAccountAsync_FormattedBankAccount_SendsTheDigitsAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out var handler);

        // Act
        await sut.SearchByBankAccountAsync("PL 61 1090 1014 0000 0712 1981 2874", Date.AddHours(23), TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/search/bank-account/61109010140000071219812874?date=2026-11-02"));
    }

    [Fact]
    public async Task SearchByBankAccountAsync_Success_MapsTheSubjectAndTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out _);

        // Act
        var result = await sut.SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectsSearchResult([MinistryOfFinance], "R43vM-99alln9", new DateTimeOffset(2026, 11, 20, 22, 45, 21, TimeSpan.FromHours(1))));
        result.Value.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task SearchByBankAccountAsync_NotOnTheList_ReturnsNotFoundWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account-not-found.json"), out _);

        // Act
        var result = await sut.SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.RequestId.ShouldBe("Meupq-99alll1");
        error.RequestDateTime.ShouldBe(new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1)));
        error.Message.ShouldBe("MF White List API has no subject on the list with the account number on the requested date.");
    }

    [Theory]
    [InlineData("61109010140000071219812875")] // a wrong check digit
    [InlineData("6110901014000007121981287")] // too short
    [InlineData("DE61109010140000071219812874")] // another country's prefix
    public async Task SearchByBankAccountAsync_InvalidBankAccount_ReturnsMaskedValidationErrorAndSendsNothing(string bankAccount)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountAsync(bankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("bankAccount");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask(bankAccount));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByBankAccountAsync_InvalidBankAccountAndDate_ReturnsBothErrors()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountAsync("61109010140000071219812875", Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.GetType()).ShouldBe([typeof(ValidationError<string>), typeof(ValidationError<DateTime>)]);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesOutOfRange))]
    public async Task SearchByBankAccountAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountAsync(BankAccount, date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(date.Date);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesInRange))]
    public async Task SearchByBankAccountAsync_DateAtTheBounds_SendsTheRequest(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-account.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountAsync(BankAccount, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBe($"?date={date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task SearchByBankAccountAsync_DailyLimit_ReturnsRateLimitErrorWithTheCode()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        // No Retry-After: the time to the next midnight in Warsaw, 12 hours after Now.
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Message.ShouldContain("search/bank-account/{account}");
    }

    [Theory]
    [MemberData(nameof(MetricCases), "search-bank-account.json", "search-bank-account-not-found.json")]
    public async Task SearchByBankAccountAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.search.bank_account");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Fact]
    public async Task SearchByBankAccountAsync_InvalidDate_RecordsTheValidationErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture("search-bank-account.json"), out _, metrics: metrics);

        // Act
        await sut.SearchByBankAccountAsync(BankAccount, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(ValidationError.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(LeakAnswers), "search-bank-account.json", "search-bank-account-not-found.json")]
    public async Task SearchByBankAccountAsync_AnyAnswer_LeaksNoBankAccount(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", BankAccount)), out _, telemetry);

        // Act
        var result = await sut.SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, BankAccount);
    }

    [Fact]
    public async Task SearchByBankAccountAsync_InvalidBankAccount_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-bank-account.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByBankAccountAsync("61109010140000071219812875", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, "61109010140000071219812875");
    }
}
