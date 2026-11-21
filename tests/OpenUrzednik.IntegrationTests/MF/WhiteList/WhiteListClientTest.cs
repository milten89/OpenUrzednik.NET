using OpenUrzednik.Core.Errors;
using OpenUrzednik.IntegrationTests.Nbp;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList;

// Explicit tests against the ministry's test host (WhiteListOptions.TestApiUrl) with its official fictitious test data, never production,
// so they spend no production quota (ADR-0023). Each test makes one request.
public class WhiteListClientTest : IClassFixture<NbpHttpClientFixture>
{
    // The test host's test subject "Nazwa Firmy 1" (Czynny).
    private const string TestNip = "3245174504";
    // A 9-digit REGON from the test data (the 14-digit ones fail the API's own check, WL-107).
    private const string TestRegon = "216973362";
    // The Ministry of Finance: on the production list, not on the test host.
    private const string NipNotOnTheTestHost = "5260250274";
    // A fake account number with a valid check digit: no test subject has an account.
    private const string AccountNotOnTheTestHost = "61109010140000071219812874";

    private readonly WhiteListClient _client;

    public WhiteListClientTest(NbpHttpClientFixture fixture)
        => _client = new WhiteListClient(fixture.HttpClient, new WhiteListOptions { ApiUrl = WhiteListOptions.TestApiUrl });

    [ManualFact]
    public async Task SearchByNipAsync_TestSubject_ReturnsItWithTheProof()
    {
        // Act
        var result = await _client.SearchByNipAsync(TestNip, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Subject.Name.ShouldBe("Nazwa Firmy 1");
        result.Value.Subject.Nip.ShouldBe(TestNip);
        result.Value.Subject.VatStatus.ShouldBe(VatStatus.Active);
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task SearchByRegonAsync_TestSubject_ReturnsItWithTheProof()
    {
        // Act
        var result = await _client.SearchByRegonAsync(TestRegon, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Subject.Regon.ShouldBe(TestRegon);
        result.Value.Subject.Name.ShouldNotBeNullOrWhiteSpace();
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task SearchByNipAsync_NotOnTheTestList_ReturnsNotFoundWithTheProof()
    {
        // Act
        var result = await _client.SearchByNipAsync(NipNotOnTheTestHost, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.ShouldBeAssignableTo<NotFoundError>();
        ShouldBeAProof(error.RequestId, error.RequestDateTime);
    }

    [ManualFact]
    public async Task SearchByBankAccountAsync_NotOnTheTestList_ReturnsNotFoundWithTheProof()
    {
        // Act
        var result = await _client.SearchByBankAccountAsync(AccountNotOnTheTestHost, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        ShouldBeAProof(error.RequestId, error.RequestDateTime);
    }

    [ManualFact]
    public async Task SearchByNipAsync_FirstDayOfTheRange_IsAccepted()
    {
        // Arrange: 1 January five years back, the earliest date the client accepts and the API answers
        var firstDay = new DateTime(WarsawToday().Year, 1, 1).AddYears(-5);

        // Act
        var result = await _client.SearchByNipAsync(TestNip, firstDay, TestContext.Current.CancellationToken);

        // Assert: the subject or not-on-the-list, never the API's WL-118
        if (result.IsFailure)
            result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
    }

    [ManualFact]
    public async Task SearchByNipsAsync_TestSubjectUnknownNipAndDuplicate_ReturnsAnEntryPerNipWithTheProof()
    {
        // Act
        var result = await _client.SearchByNipsAsync([TestNip, NipNotOnTheTestHost, TestNip], Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entries = result.Value.Entries;
        entries.Select(e => e.Identifier).ShouldBe([TestNip, NipNotOnTheTestHost, TestNip]);
        entries[0].Subjects.ShouldHaveSingleItem().Name.ShouldBe("Nazwa Firmy 1");
        entries[2].ShouldBe(entries[0]);
        entries[1].Error.ShouldBeOfType<WhiteListNotFoundError>().RequestId.ShouldBe(result.Value.RequestId);
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task SearchByRegonsAsync_TestSubject_ReturnsItsEntryWithTheProof()
    {
        // Act
        var result = await _client.SearchByRegonsAsync([TestRegon], Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entry = result.Value.Entries.ShouldHaveSingleItem();
        entry.IsSuccess.ShouldBeTrue();
        entry.Subjects.ShouldHaveSingleItem().Regon.ShouldBe(TestRegon);
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task SearchByBankAccountsAsync_NotOnTheTestList_ReturnsANotFoundEntryWithTheProof()
    {
        // Act
        var result = await _client.SearchByBankAccountsAsync([AccountNotOnTheTestHost], Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entry = result.Value.Entries.ShouldHaveSingleItem();
        entry.Identifier.ShouldBe(AccountNotOnTheTestHost);
        var error = entry.Error.ShouldBeOfType<WhiteListNotFoundError>();
        ShouldBeAProof(error.RequestId, error.RequestDateTime);
    }

    [ManualFact]
    public async Task CheckNipBankAccountAsync_TestSubjectWithAnAccountItDoesNotHave_ReturnsFalseWithTheProof()
    {
        // Act: the test subjects have no accounts, so the test host can only answer NIE (TAK is captured from production)
        var result = await _client.CheckNipBankAccountAsync(TestNip, AccountNotOnTheTestHost, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccountAssigned.ShouldBeFalse();
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task CheckNipBankAccountAsync_NipNotOnTheTestList_ReturnsFalseNotNotFound()
    {
        // Act
        var result = await _client.CheckNipBankAccountAsync(NipNotOnTheTestHost, AccountNotOnTheTestHost, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccountAssigned.ShouldBeFalse();
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task CheckRegonBankAccountAsync_TestSubjectWithAnAccountItDoesNotHave_ReturnsFalseWithTheProof()
    {
        // Act
        var result = await _client.CheckRegonBankAccountAsync(TestRegon, AccountNotOnTheTestHost, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccountAssigned.ShouldBeFalse();
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    // The answer came minutes ago, in Warsaw time (+01:00 or +02:00).
    private static void ShouldBeAProof(string requestId, DateTimeOffset requestDateTime)
    {
        requestId.ShouldNotBeNullOrWhiteSpace();
        requestDateTime.Offset.ShouldBeOneOf(TimeSpan.FromHours(1), TimeSpan.FromHours(2));
        (DateTimeOffset.UtcNow - requestDateTime).Duration().ShouldBeLessThan(TimeSpan.FromMinutes(10));
    }

    private static DateTime Yesterday() => WarsawToday().AddDays(-1);

    private static DateTime WarsawToday()
    {
        // .NET Framework only knows the Windows time zone id.
#if NET
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
#else
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
#endif
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, warsaw).Date;
    }
}
