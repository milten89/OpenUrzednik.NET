using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// Tables A and C published before 2004-05-04 have "country" and "symbol" instead of "currency": CurrencyName is empty.
// The fixtures keep the first 3 rates of each table.
public partial class NbpExchangeRateTableClientWireMockTest
{
    [Fact]
    public async Task GetAsync_DateBefore20040504_MapsWithEmptyCurrencyNames()
    {
        // Arrange
        var date = new DateTime(2002, 1, 2);
        var path = $"{BasePath}/a/{date.ToIso()}";
        _server.GivenFixture(path, "tables-a-date-2002.json");

        // Act
        var result = await CreateSut().GetAsync(TableType.A, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new ExchangeRateTable("1/A/NBP/2002", date, [
            new TableRate(string.Empty, "AUD", 2.0227m),
            new TableRate(string.Empty, "ATS", 0.2580m),
            new TableRate(string.Empty, "BEF", 0.087992m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_DateRangeAcross20040504_MapsCurrencyNamesFromTheNewFormatOnly()
    {
        // Arrange
        var from = new DateTime(2004, 4, 29);
        var to = new DateTime(2004, 5, 5);
        var path = $"{BasePath}/a/{from.ToIso()}/{to.ToIso()}";
        _server.GivenFixture(path, "tables-a-range-2004.json");

        // Act
        var result = await CreateSut().GetAsync(TableType.A, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([
            new ExchangeRateTable("84/A/NBP/2004", new DateTime(2004, 4, 29), [
                new TableRate(string.Empty, "AUD", 2.9057m),
                new TableRate(string.Empty, "CYP", 8.1802m),
                new TableRate(string.Empty, "CZK", 0.1467m),
            ]),
            new ExchangeRateTable("85/A/NBP/2004", new DateTime(2004, 4, 30), [
                new TableRate(string.Empty, "AUD", 2.9060m),
                new TableRate(string.Empty, "CYP", 8.2042m),
                new TableRate(string.Empty, "CZK", 0.1480m),
            ]),
            new ExchangeRateTable("86/A/NBP/2004", new DateTime(2004, 5, 4), [
                new TableRate("dolar amerykański", "USD", 3.9731m),
                new TableRate("dolar australijski", "AUD", 2.8841m),
                new TableRate("dolar hongkoński", "HKD", 0.5093m),
            ]),
            new ExchangeRateTable("87/A/NBP/2004", new DateTime(2004, 5, 5), [
                new TableRate("dolar amerykański", "USD", 3.9073m),
                new TableRate("dolar australijski", "AUD", 2.8621m),
                new TableRate("dolar hongkoński", "HKD", 0.5011m),
            ]),
        ]);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellAsync_DateBefore20040504_MapsWithEmptyCurrencyNames()
    {
        // Arrange
        var date = new DateTime(2002, 1, 2);
        var path = $"{BasePath}/c/{date.ToIso()}";
        _server.GivenFixture(path, "tables-c-date-2002.json");

        // Act
        var result = await CreateSut().GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRateTable("1/C/NBP/2002", new DateTime(2001, 12, 31), date, [
            new TableBuySellRate(string.Empty, "AUD", Ask: 2.0556m, Bid: 2.0148m),
            new TableBuySellRate(string.Empty, "ATS", Ask: 0.2596m, Bid: 0.2544m),
            new TableBuySellRate(string.Empty, "BEF", Ask: 0.088557m, Bid: 0.086803m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }
}
