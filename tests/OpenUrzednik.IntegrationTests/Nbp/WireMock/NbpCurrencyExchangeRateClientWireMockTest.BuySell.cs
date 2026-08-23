using System.Net;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var path = $"{BasePath}/c/{currencyCode}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "C",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "162/C/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "bid": 3.6585,
                                "ask": 3.7325
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(currencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("162/C/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[0].Buy.ShouldBe(3.7325m);
        result.Value.Rates[0].Sell.ShouldBe(3.6585m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        const int count = 3;
        var path = $"{BasePath}/c/{currencyCode}/last/{count}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "C",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "160/C/NBP/2026",
                                "effectiveDate": "2026-08-19",
                                "bid": 3.6949,
                                "ask": 3.7695
                              },
                              {
                                "no": "161/C/NBP/2026",
                                "effectiveDate": "2026-08-20",
                                "bid": 3.6642,
                                "ask": 3.7382
                              },
                              {
                                "no": "162/C/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "bid": 3.6585,
                                "ask": 3.7325
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetBuySellTopCountAsync(currencyCode, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(count);
        result.Value.Rates[0].TableId.ShouldBe("160/C/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value.Rates[0].Buy.ShouldBe(3.7695m);
        result.Value.Rates[0].Sell.ShouldBe(3.6949m);
        result.Value.Rates[1].TableId.ShouldBe("161/C/NBP/2026");
        result.Value.Rates[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates[1].Buy.ShouldBe(3.7382m);
        result.Value.Rates[1].Sell.ShouldBe(3.6642m);
        result.Value.Rates[2].TableId.ShouldBe("162/C/NBP/2026");
        result.Value.Rates[2].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[2].Buy.ShouldBe(3.7325m);
        result.Value.Rates[2].Sell.ShouldBe(3.6585m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var path = $"{BasePath}/c/{currencyCode}/today";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "C",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "162/C/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "bid": 3.6585,
                                "ask": 3.7325
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetBuySellTodayAsync(currencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("162/C/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[0].Buy.ShouldBe(3.7325m);
        result.Value.Rates[0].Sell.ShouldBe(3.6585m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var date = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/c/{currencyCode}/{date:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "C",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "161/C/NBP/2026",
                                "effectiveDate": "2026-08-20",
                                "bid": 3.6642,
                                "ask": 3.7382
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetBuySellAsync(currencyCode, date, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("161/C/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates[0].Buy.ShouldBe(3.7382m);
        result.Value.Rates[0].Sell.ShouldBe(3.6642m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var from = new DateOnly(2026, 8, 19);
        var to = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/c/{currencyCode}/{from:O}/{to:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "C",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "160/C/NBP/2026",
                                "effectiveDate": "2026-08-19",
                                "bid": 3.6949,
                                "ask": 3.7695
                              },
                              {
                                "no": "161/C/NBP/2026",
                                "effectiveDate": "2026-08-20",
                                "bid": 3.6642,
                                "ask": 3.7382
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetBuySellAsync(currencyCode, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].TableId.ShouldBe("160/C/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value.Rates[0].Buy.ShouldBe(3.7695m);
        result.Value.Rates[0].Sell.ShouldBe(3.6949m);
        result.Value.Rates[1].TableId.ShouldBe("161/C/NBP/2026");
        result.Value.Rates[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates[1].Buy.ShouldBe(3.7382m);
        result.Value.Rates[1].Sell.ShouldBe(3.6642m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }
}
