using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// The table fixtures keep the first 3 of the 13 table C rates. Buy is the API's 'ask' rate and Sell its 'bid' rate.
public partial class NbpExchangeRateTableClientWireMockTest
{
    private static readonly BuySellExchangeRateTable Table189C = new("189/C/NBP/2026", new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 29), [
        new TableBuySellRate("dolar amerykański", "USD", Buy: 3.8814m, Sell: 3.8046m),
        new TableBuySellRate("dolar australijski", "AUD", Buy: 2.7272m, Sell: 2.6732m),
        new TableBuySellRate("dolar kanadyjski", "CAD", Buy: 2.7399m, Sell: 2.6857m),
    ]);

    private static readonly BuySellExchangeRateTable Table190C = new("190/C/NBP/2026", new DateOnly(2026, 9, 29), new DateOnly(2026, 9, 30), [
        new TableBuySellRate("dolar amerykański", "USD", Buy: 3.8910m, Sell: 3.8140m),
        new TableBuySellRate("dolar australijski", "AUD", Buy: 2.7203m, Sell: 2.6665m),
        new TableBuySellRate("dolar kanadyjski", "CAD", Buy: 2.7418m, Sell: 2.6876m),
    ]);

    private static readonly BuySellExchangeRateTable Table191C = new("191/C/NBP/2026", new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 1), [
        new TableBuySellRate("dolar amerykański", "USD", Buy: 3.8811m, Sell: 3.8043m),
        new TableBuySellRate("dolar australijski", "AUD", Buy: 2.7004m, Sell: 2.6470m),
        new TableBuySellRate("dolar kanadyjski", "CAD", Buy: 2.7365m, Sell: 2.6823m),
    ]);

    private static readonly BuySellExchangeRateTable Table192C = new("192/C/NBP/2026", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), [
        new TableBuySellRate("dolar amerykański", "USD", Buy: 3.9115m, Sell: 3.8341m),
        new TableBuySellRate("dolar australijski", "AUD", Buy: 2.7191m, Sell: 2.6653m),
        new TableBuySellRate("dolar kanadyjski", "CAD", Buy: 2.7478m, Sell: 2.6934m),
    ]);

    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithData_MapsToBuySellExchangeRateTable()
    {
        // Arrange
        var path = $"{BasePath}/c";
        _server.GivenFixture(path, "tables-c-latest.json");

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table192C);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellTopCountAsync_Returns200WithData_MapsToBuySellExchangeRateTables()
    {
        // Arrange
        const int count = 2;
        var path = $"{BasePath}/c/last/{count}";
        _server.GivenFixture(path, "tables-c-last-2.json");

        // Act
        var result = await CreateSut().GetBuySellTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([Table191C, Table192C]);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellTodayAsync_Returns200WithData_MapsToBuySellExchangeRateTable()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/c/today";
        _server.GivenFixture(path, "tables-c-date.json");

        // Act
        var result = await CreateSut().GetBuySellTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table190C);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellAsync_SpecificDate_Returns200WithData_MapsToBuySellExchangeRateTable()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/c/{date.ToIso()}";
        _server.GivenFixture(path, "tables-c-date.json");

        // Act
        var result = await CreateSut().GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table190C);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_Returns200WithData_MapsToBuySellExchangeRateTables()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 29);
        var to = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/c/{from.ToIso()}/{to.ToIso()}";
        _server.GivenFixture(path, "tables-c-range.json");

        // Act
        var result = await CreateSut().GetBuySellAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([Table189C, Table190C]);
        _server.ShouldHaveReceivedGet(path);
    }
}
