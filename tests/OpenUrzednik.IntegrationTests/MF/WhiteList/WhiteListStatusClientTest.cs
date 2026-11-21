using OpenUrzednik.IntegrationTests.Nbp;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList;

// Explicit tests of the WL2 status client against the ministry's test host (WhiteListOptions.TestApiUrl) with its official fictitious test
// data, never production (ADR-0023). Each test makes one request; WL2's quota is 50,000 a day.
public class WhiteListStatusClientTest : IClassFixture<NbpHttpClientFixture>
{
    // Test subjects: active, not registered, and one that isn't in the VAT register.
    private const string ActiveNip = "1476620167";
    private const string NotRegisteredNip = "2242366993";
    private const string NotInRegisterNip = "0074370710";

    private readonly WhiteListStatusClient _client;

    public WhiteListStatusClientTest(NbpHttpClientFixture fixture)
        => _client = new WhiteListStatusClient(fixture.HttpClient, new WhiteListOptions { ApiUrl = WhiteListOptions.TestApiUrl });

    [ManualFact]
    public async Task GetVatStatusByNipAsync_ActiveNipOnADate_ReturnsActiveWithTheProof()
    {
        // Arrange
        var date = Yesterday();

        // Act
        var result = await _client.GetVatStatusByNipAsync(ActiveNip, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.VatStatus.ShouldBe(VatStatus.Active);
        result.Value.Date.ShouldBe(date);
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task GetVatStatusByNipAsync_NoDate_ReturnsTodayInTheResult()
    {
        // Act
        var result = await _client.GetVatStatusByNipAsync(ActiveNip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.VatStatus.ShouldBe(VatStatus.Active);
        // The answer's day is Warsaw's today (the test can straddle midnight, so accept yesterday's too).
        result.Value.Date.ShouldBeOneOf(WarsawToday(), WarsawToday().AddDays(-1));
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task GetVatStatusByNipAsync_NotRegisteredNip_ReturnsNotRegistered()
    {
        // Act
        var result = await _client.GetVatStatusByNipAsync(NotRegisteredNip, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.VatStatus.ShouldBe(VatStatus.NotRegistered);
        ShouldBeAProof(result.Value.RequestId, result.Value.RequestDateTime);
    }

    [ManualFact]
    public async Task GetVatStatusByNipAsync_NipNotInTheRegister_ReturnsNotFoundWithTheProof()
    {
        // Act
        var result = await _client.GetVatStatusByNipAsync(NotInRegisterNip, Yesterday(), TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        ShouldBeAProof(error.RequestId, error.RequestDateTime);
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
