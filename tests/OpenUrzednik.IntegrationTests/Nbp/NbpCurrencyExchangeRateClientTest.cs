using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp;

public class NbpCurrencyExchangeRateClientTest : IClassFixture<NbpHttpClientFixture>
{
    private readonly NbpCurrencyExchangeRateClient _client;

    public NbpCurrencyExchangeRateClientTest(NbpHttpClientFixture fixture)
        => _client = new NbpCurrencyExchangeRateClient(fixture.HttpClient);

    [ManualTheory]
    [InlineData("USD", TableType.A)]
    [InlineData("AFN", TableType.B)]
    public async Task GetLatestAsync_ReturnsLatestRate(string currency, TableType table)
    {
        // Arrange

        // Act
        var result = await _client.GetLatestAsync(currency, table, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyCode.ShouldBe(currency);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].Price.ShouldBeGreaterThan(0m);
    }

    [ManualTheory]
    [InlineData("USD", TableType.A)]
    [InlineData("AFN", TableType.B)]
    public async Task GetTopCountAsync_ReturnsRequestedNumberOfRates(string currency, TableType table)
    {
        // Arrange
        const int topCount = 3;

        // Act
        var result = await _client.GetTopCountAsync(currency, topCount, table, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyCode.ShouldBe(currency);
        result.Value.Rates.Count.ShouldBe(topCount);
    }

    [ManualFact]
    public async Task GetAsync_DateRangeAtRatesLimit_IsAcceptedByTheApi()
    {
        // Arrange
        var from = new DateOnly(2025, 1, 1);
        var to = from.AddDays(367);

        // Act
        var result = await _client.GetAsync("USD", from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Rates.Count.ShouldBeGreaterThan(200);
    }

    // Guards the limits in DateRangeValidator and TopCountValidator: one step past each must be rejected by the API itself.
    [ManualTheory]
    [InlineData("exchangerates/rates/a/usd/2025-01-01/2026-01-04/", "367")]
    [InlineData("cenyzlota/2025-01-01/2026-01-04/", "367")]
    [InlineData("exchangerates/tables/a/2026-01-01/2026-04-05/", "93")]
    [InlineData("exchangerates/rates/a/usd/last/256/", "255")]
    [InlineData("exchangerates/tables/a/last/68/", "67")]
    [InlineData("exchangerates/tables/c/last/68/", "67")]
    [InlineData("exchangerates/tables/b/last/15/", "14")]
    public async Task Api_OneStepPastLimit_ReturnsBadRequest(string path, string limit)
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri(NbpOptions.DefaultApiUrl) };

        // Act
        using var response = await httpClient.GetAsync(path, TestContext.Current.CancellationToken);
#if NET
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
#else
        var body = await response.Content.ReadAsStringAsync();
#endif

        // Assert
        ((int)response.StatusCode).ShouldBe(400);
        body.ShouldContain(limit);
    }
}
