using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Currency;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    private const string CurrencyCode = "USD";
    private const string CurrencyName = "dolar amerykański";

    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/a/{CurrencyCode}";
        _server.GivenFixture(path, "rates-a-latest.json");

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyRate("191/A/NBP/2026", new DateOnly(2026, 10, 1), 3.8762m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/a/{CurrencyCode}/last/{count}";
        _server.GivenFixture(path, "rates-a-last-3.json");

        // Act
        var result = await CreateSut().GetTopCountAsync(CurrencyCode, count, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyRate("189/A/NBP/2026", new DateOnly(2026, 9, 29), 3.8537m),
            new CurrencyRate("190/A/NBP/2026", new DateOnly(2026, 9, 30), 3.8449m),
            new CurrencyRate("191/A/NBP/2026", new DateOnly(2026, 10, 1), 3.8762m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/a/{CurrencyCode}/today";
        _server.GivenFixture(path, "rates-a-date.json");

        // Act
        var result = await CreateSut().GetTodayAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyRate("190/A/NBP/2026", new DateOnly(2026, 9, 30), 3.8449m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/a/{CurrencyCode}/{date:O}";
        _server.GivenFixture(path, "rates-a-date.json");

        // Act
        var result = await CreateSut().GetAsync(CurrencyCode, date, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyRate("190/A/NBP/2026", date, 3.8449m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 28);
        var to = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/a/{CurrencyCode}/{from:O}/{to:O}";
        _server.GivenFixture(path, "rates-a-range.json");

        // Act
        var result = await CreateSut().GetAsync(CurrencyCode, from, to, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CurrencyExchangeRates(CurrencyName, CurrencyCode, [
            new CurrencyRate("188/A/NBP/2026", new DateOnly(2026, 9, 28), 3.8478m),
            new CurrencyRate("189/A/NBP/2026", new DateOnly(2026, 9, 29), 3.8537m),
            new CurrencyRate("190/A/NBP/2026", new DateOnly(2026, 9, 30), 3.8449m),
        ]));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithNullRates_ReturnsSerializationError()
    {
        // Arrange
        _server.GivenJson($"{BasePath}/a/{CurrencyCode}", """{"table":"A","currency":"dolar amerykański","code":"USD","rates":null}""");

        // Act
        var result = await CreateSut().GetLatestAsync(CurrencyCode, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>()
            .Message.ShouldBe("NBP API response is missing 'rates'.");
    }
}
