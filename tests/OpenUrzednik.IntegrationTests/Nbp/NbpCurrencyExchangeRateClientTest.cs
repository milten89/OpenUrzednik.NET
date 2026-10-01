using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp;

public class NbpCurrencyExchangeRateClientTest : IClassFixture<NbpHttpClientFixture>
{
    private readonly NbpCurrencyExchangeRateClient _client;

    public NbpCurrencyExchangeRateClientTest(NbpHttpClientFixture fixture)
        => _client = new NbpCurrencyExchangeRateClient(fixture.HttpClient, new NbpUrlBuilderFactory());

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
}
