using OpenUrzednik.Nbp.Currency;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/c/{CurrencyCode}";
        _server.GivenFixture(path, "rates-c-latest.json");

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(CurrencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyBuySellRate("192/C/NBP/2026", new DateOnly(2026, 10, 2), Ask: 3.9115m, Bid: 3.8341m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/c/{CurrencyCode}/last/{count}";
        _server.GivenFixture(path, "rates-c-last-3.json");

        // Act
        var result = await CreateSut().GetBuySellTopCountAsync(CurrencyCode, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyBuySellRate("190/C/NBP/2026", new DateOnly(2026, 9, 30), Ask: 3.8910m, Bid: 3.8140m),
            new CurrencyBuySellRate("191/C/NBP/2026", new DateOnly(2026, 10, 1), Ask: 3.8811m, Bid: 3.8043m),
            new CurrencyBuySellRate("192/C/NBP/2026", new DateOnly(2026, 10, 2), Ask: 3.9115m, Bid: 3.8341m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/c/{CurrencyCode}/today";
        _server.GivenFixture(path, "rates-c-date.json");

        // Act
        var result = await CreateSut().GetBuySellTodayAsync(CurrencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyBuySellRate("190/C/NBP/2026", new DateOnly(2026, 9, 30), Ask: 3.8910m, Bid: 3.8140m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/c/{CurrencyCode}/{date.ToIso()}";
        _server.GivenFixture(path, "rates-c-date.json");

        // Act
        var result = await CreateSut().GetBuySellAsync(CurrencyCode, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyBuySellRate("190/C/NBP/2026", date, Ask: 3.8910m, Bid: 3.8140m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 28);
        var to = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/c/{CurrencyCode}/{from.ToIso()}/{to.ToIso()}";
        _server.GivenFixture(path, "rates-c-range.json");

        // Act
        var result = await CreateSut().GetBuySellAsync(CurrencyCode, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new BuySellExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyBuySellRate("188/C/NBP/2026", new DateOnly(2026, 9, 28), Ask: 3.8744m, Bid: 3.7976m),
            new CurrencyBuySellRate("189/C/NBP/2026", new DateOnly(2026, 9, 29), Ask: 3.8814m, Bid: 3.8046m),
            new CurrencyBuySellRate("190/C/NBP/2026", new DateOnly(2026, 9, 30), Ask: 3.8910m, Bid: 3.8140m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }
}
