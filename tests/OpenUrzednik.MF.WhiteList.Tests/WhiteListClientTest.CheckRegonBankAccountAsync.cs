using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public sealed partial class WhiteListClientTest
{
    [Fact]
    public void CheckRegonBankAccountAsync_NullRegon_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.CheckRegonBankAccountAsync(null!, BankAccount, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("regon");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void CheckRegonBankAccountAsync_NullBankAccount_ThrowsBeforeReturningATask()
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => { _ = sut.CheckRegonBankAccountAsync(Regon, null!, Date, TestContext.Current.CancellationToken); });

        // Assert
        exception.ParamName.ShouldBe("bankAccount");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task CheckRegonBankAccountAsync_FormattedValues_SendsTheDigitsAndTheDate()
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        await sut.CheckRegonBankAccountAsync("593-728-16-2", "PL61 1090 1014 0000 0712 1981 2874", Date.AddHours(23), TestContext.Current.CancellationToken);

        // Assert
        var request = handler.Request.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri.ShouldBe(new Uri("https://wykazpodatnikowvat.mf.gov.pl/api/check/regon/593728162/bank-account/61109010140000071219812874?date=2026-11-02"));
    }

    [Fact]
    public async Task CheckRegonBankAccountAsync_Assigned_ReturnsTrueWithTheProof()
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out _);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new AccountCheckResult(true, "inirF-99b2nhh", CheckAnsweredLater));
        result.Value.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Theory]
    [InlineData("check-regon-not-assigned.json", "N4ZPn-99b2nhg")]
    public async Task CheckRegonBankAccountAsync_NotAssigned_ReturnsFalseWithTheProof(string fixture, string requestId)
    {
        // Arrange
        var sut = CreateSut(Fixture(fixture), out _);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new AccountCheckResult(false, requestId, CheckAnswered));
    }

    [Theory]
    [InlineData("""{"result":{"accountAssigned":"tak","requestDateTime":"21-11-2026 18:14:16","requestId":"r"}}""")]
    [InlineData("""{"result":{"accountAssigned":"MAYBE","requestDateTime":"21-11-2026 18:14:16","requestId":"r"}}""")]
    [InlineData("""{"result":{"accountAssigned":null,"requestDateTime":"21-11-2026 18:14:16","requestId":"r"}}""")]
    public async Task CheckRegonBankAccountAsync_UnknownAccountAssigned_ReturnsSerializationError(string body)
    {
        // Arrange
        var sut = CreateSut(Json(HttpStatusCode.OK, body), out _);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Theory]
    [InlineData("593728163")] // a wrong check digit
    [InlineData("59372816")] // too short
    [InlineData("DE593728162")] // another country's prefix
    public async Task CheckRegonBankAccountAsync_InvalidRegon_ReturnsMaskedValidationErrorAndSendsNothing(string regon)
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("regon");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask(regon));
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData("61109010140000071219812875")] // a wrong check digit
    [InlineData("6110901014000007121981287")] // too short
    public async Task CheckRegonBankAccountAsync_InvalidBankAccount_ReturnsMaskedValidationErrorAndSendsNothing(string account)
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, account, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Name.ShouldBe("bankAccount");
        error.Value.ShouldBe(OpenUrzednik.Privacy.SensitiveValue.Mask(account));
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task CheckRegonBankAccountAsync_EverythingInvalid_ReturnsAllErrorsTogether()
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var result = await sut.CheckRegonBankAccountAsync("593728163", "61109010140000071219812875", Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.GetType()).ShouldBe([typeof(ValidationError<string>), typeof(ValidationError<string>), typeof(ValidationError<DateTime>)]);
        result.Errors.OfType<ValidationError<string>>().Select(e => e.Name).ShouldBe(["regon", "bankAccount"]);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesOutOfRange))]
    public async Task CheckRegonBankAccountAsync_DateOutOfRange_ReturnsValidationErrorAndSendsNothing(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(date.Date);
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(DatesInRange))]
    public async Task CheckRegonBankAccountAsync_DateAtTheBounds_SendsTheRequest(DateTime date)
    {
        // Arrange
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out var handler);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        handler.Request.ShouldNotBeNull().RequestUri!.Query.ShouldBe($"?date={date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task CheckRegonBankAccountAsync_DailyLimit_ReturnsRateLimitErrorWithTheCode()
    {
        // Arrange
        var sut = CreateSut(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json"), out _);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Message.ShouldContain("check/regon/{regon}/bank-account/{account}");
    }

    [Theory]
    [MemberData(nameof(CheckMetricCasesRegon))]
    public async Task CheckRegonBankAccountAsync_Completed_RecordsTheOperationWithItsErrorType(string fixture, int status, string? errorType)
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture((HttpStatusCode)status, fixture), out _, metrics: metrics);

        // Act
        await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["openurzednik.operation"].ShouldBe("mf_white_list.check.regon_bank_account");
        tags.TryGetValue("error.type", out var recorded).ShouldBe(errorType is not null);
        recorded.ShouldBe(errorType);
    }

    public static TheoryData<string, int, string?> CheckMetricCasesRegon => CheckMetricCases("check-regon-assigned.json", "check-regon-not-assigned.json");

    [Fact]
    public async Task CheckRegonBankAccountAsync_InvalidDate_RecordsTheValidationErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out _, metrics: metrics);

        // Act
        await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(ValidationError.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(CheckLeakAnswersRegon))]
    public async Task CheckRegonBankAccountAsync_AnyAnswer_LeaksNeitherRegonNorAccount(int status, string body)
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Json((HttpStatusCode)status, body.Replace("{id}", Regon + " " + BankAccount)), out _, telemetry);

        // Act
        var result = await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotLeak(telemetry, Regon, BankAccount);
    }

    public static TheoryData<int, string> CheckLeakAnswersRegon => CheckLeakAnswers("check-regon-assigned.json", "check-regon-not-assigned.json");

    [Fact]
    public async Task CheckRegonBankAccountAsync_InvalidValues_LeaksNothing()
    {
        // Arrange
        var telemetry = new RecordingTelemetry();
        var sut = CreateSut(Fixture("check-regon-assigned.json"), out _, telemetry);

        // Act
        var result = await sut.CheckRegonBankAccountAsync("593728163", "61109010140000071219812875", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ShouldNotLeak(telemetry, "593728163", "61109010140000071219812875");
    }
}
