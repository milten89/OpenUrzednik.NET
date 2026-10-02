using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Table;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// The table fixtures keep the first 3 rates (of 32 in table A and 116 in table B).
public partial class NbpExchangeRateTableClientWireMockTest
{
    private static readonly ExchangeRateTable Table189A = new("189/A/NBP/2026", new DateOnly(2026, 9, 29), [
        new TableRate("bat (Tajlandia)", "THB", 0.1147m),
        new TableRate("dolar amerykański", "USD", 3.8537m),
        new TableRate("dolar australijski", "AUD", 2.6941m),
    ]);

    private static readonly ExchangeRateTable Table190A = new("190/A/NBP/2026", new DateOnly(2026, 9, 30), [
        new TableRate("bat (Tajlandia)", "THB", 0.1146m),
        new TableRate("dolar amerykański", "USD", 3.8449m),
        new TableRate("dolar australijski", "AUD", 2.6818m),
    ]);

    private static readonly ExchangeRateTable Table191A = new("191/A/NBP/2026", new DateOnly(2026, 10, 1), [
        new TableRate("bat (Tajlandia)", "THB", 0.1150m),
        new TableRate("dolar amerykański", "USD", 3.8762m),
        new TableRate("dolar australijski", "AUD", 2.6894m),
    ]);

    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToExchangeRateTable()
    {
        // Arrange
        var path = $"{BasePath}/a";
        _server.GivenFixture(path, "tables-a-latest.json");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table191A);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetLatestAsync_TableB_Returns200WithData_MapsToExchangeRateTable()
    {
        // Arrange
        var path = $"{BasePath}/b";
        _server.GivenFixture(path, "tables-b-latest.json");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new ExchangeRateTable("039/B/NBP/2026", new DateOnly(2026, 9, 30), [
            new TableRate("afgani (Afganistan)", "AFN", 0.058988m),
            new TableRate("ariary (Madagaskar)", "MGA", 0.000875m),
            new TableRate("balboa (Panama)", "PAB", 3.8449m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTopCountAsync_Returns200WithData_MapsToExchangeRateTables()
    {
        // Arrange
        const int count = 2;
        var path = $"{BasePath}/a/last/{count}";
        _server.GivenFixture(path, "tables-a-last-2.json");

        // Act
        var result = await CreateSut().GetTopCountAsync(TableType.A, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([Table190A, Table191A]);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTodayAsync_Returns200WithData_MapsToExchangeRateTable()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/a/today";
        _server.GivenFixture(path, "tables-a-date.json");

        // Act
        var result = await CreateSut().GetTodayAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table190A);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns200WithData_MapsToExchangeRateTable()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/a/{date:yyyy-MM-dd}";
        _server.GivenFixture(path, "tables-a-date.json");

        // Act
        var result = await CreateSut().GetAsync(TableType.A, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Table190A);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns200WithData_MapsToExchangeRateTables()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 29);
        var to = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/a/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}";
        _server.GivenFixture(path, "tables-a-range.json");

        // Act
        var result = await CreateSut().GetAsync(TableType.A, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([Table189A, Table190A]);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithNullRates_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a", """[{"table":"A","no":"191/A/NBP/2026","effectiveDate":"2026-10-01","rates":null}]""");

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>()
            .Message.ShouldBe("NBP API response is missing 'rates'.");
    }
}
