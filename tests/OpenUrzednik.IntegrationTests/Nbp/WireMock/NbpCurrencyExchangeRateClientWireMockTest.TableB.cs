using System.Net;

using OpenUrzednik.Nbp.Table;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    private const string TableBCurrencyCode = "AFN";
    private const string TableBCurrencyName = "afgani (Afganistan)";

    private void GivenTableBResponse(string path, string fixture)
        => _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(NbpFixtures.Load(fixture)));

    [Fact]
    public async Task GetLatestAsync_TableB_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/b/{TableBCurrencyCode}";
        GivenTableBResponse(path, "rates-b-latest.json");

        // Act
        var result = await CreateSut().GetLatestAsync(TableBCurrencyCode, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe(TableBCurrencyName);
        result.Value.CurrencyCode.ShouldBe(TableBCurrencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("039/B/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 9, 30));
        result.Value.Rates[0].Price.ShouldBe(0.058988m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTopCountAsync_TableB_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/b/{TableBCurrencyCode}/last/{count}";
        GivenTableBResponse(path, "rates-b-last-3.json");

        // Act
        var result = await CreateSut().GetTopCountAsync(TableBCurrencyCode, count, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe(TableBCurrencyName);
        result.Value.CurrencyCode.ShouldBe(TableBCurrencyCode);
        result.Value.Rates.Count.ShouldBe(count);
        result.Value.Rates[0].TableId.ShouldBe("037/B/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 9, 16));
        result.Value.Rates[0].Price.ShouldBe(0.058369m);
        result.Value.Rates[2].TableId.ShouldBe("039/B/NBP/2026");
        result.Value.Rates[2].PublicationDate.ShouldBe(new DateOnly(2026, 9, 30));
        result.Value.Rates[2].Price.ShouldBe(0.058988m);
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == path);
    }

    [Fact]
    public async Task GetTodayAsync_TableB_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/b/{TableBCurrencyCode}/today";
        GivenTableBResponse(path, "rates-b-date.json");

        // Act
        var result = await CreateSut().GetTodayAsync(TableBCurrencyCode, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyCode.ShouldBe(TableBCurrencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 9, 30));
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == path);
    }

    [Fact]
    public async Task GetAsync_TableB_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/b/{TableBCurrencyCode}/{date:O}";
        GivenTableBResponse(path, "rates-b-date.json");

        // Act
        var result = await CreateSut().GetAsync(TableBCurrencyCode, date, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe(TableBCurrencyName);
        result.Value.CurrencyCode.ShouldBe(TableBCurrencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("039/B/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(date);
        result.Value.Rates[0].Price.ShouldBe(0.058988m);
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == path);
    }

    [Fact]
    public async Task GetAsync_TableB_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 8);
        var to = new DateOnly(2026, 9, 24);
        var path = $"{BasePath}/b/{TableBCurrencyCode}/{from:O}/{to:O}";
        GivenTableBResponse(path, "rates-b-range.json");

        // Act
        var result = await CreateSut().GetAsync(TableBCurrencyCode, from, to, TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe(TableBCurrencyName);
        result.Value.CurrencyCode.ShouldBe(TableBCurrencyCode);
        result.Value.Rates.Count.ShouldBe(3);
        result.Value.Rates[0].TableId.ShouldBe("036/B/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 9, 9));
        result.Value.Rates[0].Price.ShouldBe(0.057746m);
        result.Value.Rates[2].TableId.ShouldBe("038/B/NBP/2026");
        result.Value.Rates[2].PublicationDate.ShouldBe(new DateOnly(2026, 9, 23));
        result.Value.Rates[2].Price.ShouldBe(0.058927m);
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == path);
    }
}
