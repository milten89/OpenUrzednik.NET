using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    private const string TableBCurrencyCode = "AFN";
    private const string TableBCurrencyName = "afgani (Afganistan)";

    [Fact]
    public async Task GetLatestAsync_TableB_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/b/{TableBCurrencyCode}";
        _server.GivenFixture(path, "rates-b-latest.json");

        // Act
        var result = await CreateSut().GetLatestAsync(TableBCurrencyCode, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(TableBCurrencyName, TableBCurrencyCode, [
            new CurrencyRate("039/B/NBP/2026", new DateOnly(2026, 9, 30), 0.058988m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTopCountAsync_TableB_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/b/{TableBCurrencyCode}/last/{count}";
        _server.GivenFixture(path, "rates-b-last-3.json");

        // Act
        var result = await CreateSut().GetTopCountAsync(TableBCurrencyCode, count, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(TableBCurrencyName, TableBCurrencyCode, [
            new CurrencyRate("037/B/NBP/2026", new DateOnly(2026, 9, 16), 0.058369m),
            new CurrencyRate("038/B/NBP/2026", new DateOnly(2026, 9, 23), 0.058927m),
            new CurrencyRate("039/B/NBP/2026", new DateOnly(2026, 9, 30), 0.058988m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTodayAsync_TableB_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/b/{TableBCurrencyCode}/today";
        _server.GivenFixture(path, "rates-b-date.json");

        // Act
        var result = await CreateSut().GetTodayAsync(TableBCurrencyCode, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(TableBCurrencyName, TableBCurrencyCode, [
            new CurrencyRate("039/B/NBP/2026", new DateOnly(2026, 9, 30), 0.058988m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_TableB_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/b/{TableBCurrencyCode}/{date:O}";
        _server.GivenFixture(path, "rates-b-date.json");

        // Act
        var result = await CreateSut().GetAsync(TableBCurrencyCode, date, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(TableBCurrencyName, TableBCurrencyCode, [
            new CurrencyRate("039/B/NBP/2026", date, 0.058988m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_TableB_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 8);
        var to = new DateOnly(2026, 9, 24);
        var path = $"{BasePath}/b/{TableBCurrencyCode}/{from:O}/{to:O}";
        _server.GivenFixture(path, "rates-b-range.json");

        // Act
        var result = await CreateSut().GetAsync(TableBCurrencyCode, from, to, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(TableBCurrencyName, TableBCurrencyCode, [
            new CurrencyRate("036/B/NBP/2026", new DateOnly(2026, 9, 9), 0.057746m),
            new CurrencyRate("037/B/NBP/2026", new DateOnly(2026, 9, 16), 0.058369m),
            new CurrencyRate("038/B/NBP/2026", new DateOnly(2026, 9, 23), 0.058927m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }
}
