using OpenUrzednik.Nbp.Currency;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// NBP answers whose earliest rate was published before 2004-05-04 have "country" and "symbol" instead of "currency":
// CurrencyName is empty for the whole answer, also for the later rates of a range.
public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    [Fact]
    public async Task GetAsync_DateBefore20040504_MapsWithEmptyCurrencyName()
    {
        // Arrange
        var date = new DateTime(2002, 1, 2);
        var path = $"{BasePath}/a/{CurrencyCode}/{date.ToIso()}";
        _server.GivenFixture(path, "rates-a-date-2002.json");

        // Act
        var result = await CreateSut().GetAsync(CurrencyCode, date, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(string.Empty, CurrencyCode, [
            new CurrencyRate("1/A/NBP/2002", date, 3.9480m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_DateRangeAcross20040504_MapsAllRatesWithEmptyCurrencyName()
    {
        // Arrange
        var from = new DateTime(2004, 4, 28);
        var to = new DateTime(2004, 5, 6);
        var path = $"{BasePath}/a/{CurrencyCode}/{from.ToIso()}/{to.ToIso()}";
        _server.GivenFixture(path, "rates-a-range-2004.json");

        // Act
        var result = await CreateSut().GetAsync(CurrencyCode, from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(string.Empty, CurrencyCode, [
            new CurrencyRate("83/A/NBP/2004", new DateTime(2004, 4, 28), 4.0110m),
            new CurrencyRate("84/A/NBP/2004", new DateTime(2004, 4, 29), 4.0572m),
            new CurrencyRate("85/A/NBP/2004", new DateTime(2004, 4, 30), 4.0254m),
            new CurrencyRate("86/A/NBP/2004", new DateTime(2004, 5, 4), 3.9731m),
            new CurrencyRate("87/A/NBP/2004", new DateTime(2004, 5, 5), 3.9073m),
            new CurrencyRate("88/A/NBP/2004", new DateTime(2004, 5, 6), 3.9242m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellAsync_DateBefore20040504_MapsWithEmptyCurrencyName()
    {
        // Arrange
        var date = new DateTime(2002, 1, 2);
        var path = $"{BasePath}/c/{CurrencyCode}/{date.ToIso()}";
        _server.GivenFixture(path, "rates-c-date-2002.json");

        // Act
        var result = await CreateSut().GetBuySellAsync(CurrencyCode, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(string.Empty, CurrencyCode, [
            new CurrencyBuySellRate("1/C/NBP/2002", date, Ask: 4.0249m, Bid: 3.9451m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_WindowStartsBefore20040504_MapsLatestRateWithEmptyCurrencyName()
    {
        // Arrange
        // The 14 days before 2004-05-06: the answer starts with old-format tables, so even the 2004-05-05 rate has no name.
        var path = $"{BasePath}/c/{CurrencyCode}/2004-04-22/2004-05-05";
        _server.GivenFixture(path, "rates-c-last-before-2004.json");

        // Act
        var result = await CreateSut().GetBuySellLastPublishedBeforeAsync(CurrencyCode, new DateTime(2004, 5, 6), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(string.Empty, CurrencyCode, [
            new CurrencyBuySellRate("87/C/NBP/2004", new DateTime(2004, 5, 5), Ask: 3.9843m, Bid: 3.9055m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }
}
