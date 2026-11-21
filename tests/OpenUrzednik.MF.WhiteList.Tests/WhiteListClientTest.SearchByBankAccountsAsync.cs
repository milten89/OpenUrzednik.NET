using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    // search-bank-accounts.json answers two account numbers: one no subject on the list has and one the API rejected.
    private static readonly string[] TwoBankAccounts = [BankAccount, BankAccount2];

    [Fact]
    public void SearchByBankAccountsAsync_NullList_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByBankAccountsAsync(null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("bankAccounts");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void SearchByBankAccountsAsync_NullElement_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.SearchByBankAccountsAsync([null!, BankAccount], Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("bankAccounts");
        exception.Message.ShouldStartWith("'bankAccounts[0]' is null.");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_FormattedBankAccounts_SendsTheDigitsInOrderAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out var handler);

        // Act
        await sut.SearchByBankAccountsAsync(["PL61 1090 1014 0000 0712 1981 2874", "26-0596-2967-3518-3249-9115-3273"], Date, TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri($"https://wykazpodatnikowvat.mf.gov.pl/api/search/bank-accounts/{BankAccount},{BankAccount2}?date=2026-11-02"));
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_Success_MapsEveryEntryInOrderWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out _);

        // Act
        var result = await sut.SearchByBankAccountsAsync(TwoBankAccounts, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BatchSearchResult(
        [
            NotOnList("70506405335016096312945164", "3h7uz-99b0ck2"),
            Rejected("61101000000000000000000000", "Nieprawidłowy numer konta bankowego.", "WL-111"),
        ], "3h7uz-99b0ck2", BatchAnswered));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task SearchByBankAccountsAsync_CountOutOfRange_ReturnsCountValidationErrorAndSendsNothing(int count)
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountsAsync(Batch(count, BankAccount, BankAccount2), Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<int>>();
        error.Name.ShouldBe("bankAccounts");
        error.Value.ShouldBe(count);
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_InvalidBankAccounts_ReturnsAMaskedErrorPerPositionAndSendsNothing()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountsAsync(["61109010140000071219812875", BankAccount, BankAccount2], Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("bankAccounts[0]");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask("61109010140000071219812875"));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing()
    {
        // Arrange
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out var handler);

        // Act
        var result = await sut.SearchByBankAccountsAsync(TwoBankAccounts, MinDate.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>().Name.ShouldBe("date");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_DailyLimit_ReturnsRateLimitErrorWithTheRouteTemplate()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.SearchByBankAccountsAsync(TwoBankAccounts, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.Message.ShouldContain("search/bank-accounts/{accounts}");
    }

    [Theory]
    [MemberData(nameof(BatchMetricCases), "search-bank-accounts.json")]
    public async Task SearchByBankAccountsAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.SearchByBankAccountsAsync(TwoBankAccounts, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.search.bank_accounts");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    [Theory]
    [MemberData(nameof(BatchLeakAnswers))]
    public async Task SearchByBankAccountsAsync_AnyAnswer_LeaksNoBankAccount(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", BankAccount)), out _, telemetry);

        // Act
        var result = await sut.SearchByBankAccountsAsync([BankAccount], Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, BankAccount);
        EntryErrors(result).ShouldNotLeak(telemetry, BankAccount);
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_InvalidBankAccounts_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("search-bank-accounts.json"), out _, telemetry);

        // Act
        var result = await sut.SearchByBankAccountsAsync([BankAccount, "61109010140000071219812875"], Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, BankAccount, "61109010140000071219812875");
    }
}
