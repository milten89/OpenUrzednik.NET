#if NET
using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    private const string LeakNip = "7368125416";
    private const string LeakRegon = "593728162";
    private const string LeakBankAccount = "61109010140000071219812874";

    [Theory]
    [InlineData(nameof(IWhiteListClient.SearchByNipAsync))]
    [InlineData(nameof(IWhiteListClient.SearchByRegonAsync))]
    [InlineData(nameof(IWhiteListClient.SearchByBankAccountAsync))]
    [InlineData(nameof(IWhiteListClient.SearchByNipsAsync))]
    [InlineData(nameof(IWhiteListClient.SearchByRegonsAsync))]
    [InlineData(nameof(IWhiteListClient.SearchByBankAccountsAsync))]
    [InlineData(nameof(IWhiteListClient.CheckNipBankAccountAsync))]
    [InlineData(nameof(IWhiteListClient.CheckRegonBankAccountAsync))]
    [InlineData(nameof(IWhiteListStatusClient.GetVatStatusByNipAsync))]
    public async Task AnyMethod_ErrorEchoingTheIdentifiers_LeaksNoIdentifier(string method)
    {
        // Arrange: a real client behind the extension, and an error body that echoes every identifier, as the API's 400 answers do
        var telemetry = new RecordingTelemetry();
        var options = new OpenUrzednikTelemetry { Logger = telemetry, TraceSource = telemetry };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero));
        var body = $$"""{"message":"Nieprawidłowe dane {{LeakNip}} {{LeakRegon}} {{LeakBankAccount}}.","code":"WL-115"}""";
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body, Encoding.UTF8, "application/json") }));
        IWhiteListClient client = new WhiteListClient(httpClient, null, timeProvider, options);
        IWhiteListStatusClient statusClient = new WhiteListStatusClient(httpClient, null, timeProvider, options);
        var date = new DateOnly(2026, 11, 2);
        var token = TestContext.Current.CancellationToken;

        // Act && Assert
        switch (method)
        {
            case nameof(IWhiteListClient.SearchByNipAsync):
                ShouldBeTheEchoingAnswer(await client.SearchByNipAsync(LeakNip, date, token), telemetry, LeakNip);
                break;
            case nameof(IWhiteListClient.SearchByRegonAsync):
                ShouldBeTheEchoingAnswer(await client.SearchByRegonAsync(LeakRegon, date, token), telemetry, LeakRegon);
                break;
            case nameof(IWhiteListClient.SearchByBankAccountAsync):
                ShouldBeTheEchoingAnswer(await client.SearchByBankAccountAsync(LeakBankAccount, date, token), telemetry, LeakBankAccount);
                break;
            case nameof(IWhiteListClient.SearchByNipsAsync):
                ShouldBeTheEchoingAnswer(await client.SearchByNipsAsync([LeakNip], date, token), telemetry, LeakNip);
                break;
            case nameof(IWhiteListClient.SearchByRegonsAsync):
                ShouldBeTheEchoingAnswer(await client.SearchByRegonsAsync([LeakRegon], date, token), telemetry, LeakRegon);
                break;
            case nameof(IWhiteListClient.SearchByBankAccountsAsync):
                ShouldBeTheEchoingAnswer(await client.SearchByBankAccountsAsync([LeakBankAccount], date, token), telemetry, LeakBankAccount);
                break;
            case nameof(IWhiteListClient.CheckNipBankAccountAsync):
                ShouldBeTheEchoingAnswer(await client.CheckNipBankAccountAsync(LeakNip, LeakBankAccount, date, token), telemetry, LeakNip, LeakBankAccount);
                break;
            case nameof(IWhiteListClient.CheckRegonBankAccountAsync):
                ShouldBeTheEchoingAnswer(await client.CheckRegonBankAccountAsync(LeakRegon, LeakBankAccount, date, token), telemetry, LeakRegon, LeakBankAccount);
                break;
            default:
                ShouldBeTheEchoingAnswer(await statusClient.GetVatStatusByNipAsync(LeakNip, date, token), telemetry, LeakNip);
                break;
        }
    }

    // The request reached the API and came back as its 400 answer, which echoes the identifiers, and none of them leaked.
    private static void ShouldBeTheEchoingAnswer<T>(OpenUrzednikResult<T> result, RecordingTelemetry telemetry, params string[] identifiers)
    {
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>().WhiteListCode.ShouldBe("WL-115");
        result.ShouldNotLeak(telemetry, identifiers);
    }
}
#endif
