using System.Net;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpExchangeRateTableClientWireMockTest
{
    [Fact]
    public async Task GetBuySellLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/c";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "C",
                              "no": "162/C/NBP/2026",
                              "tradingDate": "2026-08-20",
                              "effectiveDate": "2026-08-21",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6585,
                                  "ask": 3.7325
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.2716,
                                  "ask": 4.3578
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("162/C/NBP/2026");
        result.Value.TradingDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value.Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value.Rates[0].Buy.ShouldBe(3.7325m);
        result.Value.Rates[0].Sell.ShouldBe(3.6585m);
        result.Value.Rates[1].CurrencyName.ShouldBe("euro");
        result.Value.Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value.Rates[1].Buy.ShouldBe(4.3578m);
        result.Value.Rates[1].Sell.ShouldBe(4.2716m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/c/last/{count}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "C",
                              "no": "160/C/NBP/2026",
                              "tradingDate": "2026-08-18",
                              "effectiveDate": "2026-08-19",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6949,
                                  "ask": 3.7695
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.2789,
                                  "ask": 4.3653
                                }
                              ]
                            },
                            {
                              "table": "C",
                              "no": "161/C/NBP/2026",
                              "tradingDate": "2026-08-19",
                              "effectiveDate": "2026-08-20",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6642,
                                  "ask": 3.7382
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.271,
                                  "ask": 4.3572
                                }
                              ]
                            },
                            {
                              "table": "C",
                              "no": "162/C/NBP/2026",
                              "tradingDate": "2026-08-20",
                              "effectiveDate": "2026-08-21",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6585,
                                  "ask": 3.7325
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.2716,
                                  "ask": 4.3578
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetBuySellTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(count);
        result.Value[0].TableId.ShouldBe("160/C/NBP/2026");
        result.Value[0].TradingDate.ShouldBe(new DateOnly(2026, 8, 18));
        result.Value[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value[0].Rates.Count.ShouldBe(2);
        result.Value[0].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[0].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[0].Rates[0].Buy.ShouldBe(3.7695m);
        result.Value[0].Rates[0].Sell.ShouldBe(3.6949m);
        result.Value[0].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[0].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[0].Rates[1].Buy.ShouldBe(4.3653m);
        result.Value[0].Rates[1].Sell.ShouldBe(4.2789m);
        result.Value[1].TableId.ShouldBe("161/C/NBP/2026");
        result.Value[1].TradingDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value[1].Rates.Count.ShouldBe(2);
        result.Value[1].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[1].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[1].Rates[0].Buy.ShouldBe(3.7382m);
        result.Value[1].Rates[0].Sell.ShouldBe(3.6642m);
        result.Value[1].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[1].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[1].Rates[1].Buy.ShouldBe(4.3572m);
        result.Value[1].Rates[1].Sell.ShouldBe(4.271m);
        result.Value[2].TableId.ShouldBe("162/C/NBP/2026");
        result.Value[2].TradingDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value[2].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value[2].Rates.Count.ShouldBe(2);
        result.Value[2].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[2].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[2].Rates[0].Buy.ShouldBe(3.7325m);
        result.Value[2].Rates[0].Sell.ShouldBe(3.6585m);
        result.Value[2].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[2].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[2].Rates[1].Buy.ShouldBe(4.3578m);
        result.Value[2].Rates[1].Sell.ShouldBe(4.2716m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/c/today";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "C",
                              "no": "162/C/NBP/2026",
                              "tradingDate": "2026-08-20",
                              "effectiveDate": "2026-08-21",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6585,
                                  "ask": 3.7325
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.2716,
                                  "ask": 4.3578
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetBuySellTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("162/C/NBP/2026");
        result.Value.TradingDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value.Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value.Rates[0].Buy.ShouldBe(3.7325m);
        result.Value.Rates[0].Sell.ShouldBe(3.6585m);
        result.Value.Rates[1].CurrencyName.ShouldBe("euro");
        result.Value.Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value.Rates[1].Buy.ShouldBe(4.3578m);
        result.Value.Rates[1].Sell.ShouldBe(4.2716m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetBuySellAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/c/{date:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "C",
                              "no": "161/C/NBP/2026",
                              "tradingDate": "2026-08-19",
                              "effectiveDate": "2026-08-20",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6642,
                                  "ask": 3.7382
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.271,
                                  "ask": 4.3572
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetBuySellAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.Value.TableId.ShouldBe("161/C/NBP/2026");
        result.Value.TradingDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value.PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value.Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value.Rates[0].Buy.ShouldBe(3.7382m);
        result.Value.Rates[0].Sell.ShouldBe(3.6642m);
        result.Value.Rates[1].CurrencyName.ShouldBe("euro");
        result.Value.Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value.Rates[1].Buy.ShouldBe(4.3572m);
        result.Value.Rates[1].Sell.ShouldBe(4.271m);
    }

    [Fact]
    public async Task GetBuySellAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var from = new DateOnly(2026, 8, 19);
        var to = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/c/{from:O}/{to:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "C",
                              "no": "160/C/NBP/2026",
                              "tradingDate": "2026-08-18",
                              "effectiveDate": "2026-08-19",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6949,
                                  "ask": 3.7695
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.2789,
                                  "ask": 4.3653
                                }
                              ]
                            },
                            {
                              "table": "C",
                              "no": "161/C/NBP/2026",
                              "tradingDate": "2026-08-19",
                              "effectiveDate": "2026-08-20",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "bid": 3.6642,
                                  "ask": 3.7382
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "bid": 4.271,
                                  "ask": 4.3572
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetBuySellAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].TableId.ShouldBe("160/C/NBP/2026");
        result.Value[0].TradingDate.ShouldBe(new DateOnly(2026, 8, 18));
        result.Value[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value[0].Rates.Count.ShouldBe(2);
        result.Value[0].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[0].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[0].Rates[0].Buy.ShouldBe(3.7695m);
        result.Value[0].Rates[0].Sell.ShouldBe(3.6949m);
        result.Value[0].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[0].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[0].Rates[1].Buy.ShouldBe(4.3653m);
        result.Value[0].Rates[1].Sell.ShouldBe(4.2789m);
        result.Value[1].TableId.ShouldBe("161/C/NBP/2026");
        result.Value[1].TradingDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value[1].Rates.Count.ShouldBe(2);
        result.Value[1].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[1].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[1].Rates[0].Buy.ShouldBe(3.7382m);
        result.Value[1].Rates[0].Sell.ShouldBe(3.6642m);
        result.Value[1].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[1].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[1].Rates[1].Buy.ShouldBe(4.3572m);
        result.Value[1].Rates[1].Sell.ShouldBe(4.271m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }
}
