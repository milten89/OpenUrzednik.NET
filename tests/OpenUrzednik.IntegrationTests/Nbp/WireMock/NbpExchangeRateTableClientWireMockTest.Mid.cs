using System.Net;

using OpenUrzednik.Nbp.Table;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpExchangeRateTableClientWireMockTest
{
    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/a";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "A",
                              "no": "162/A/NBP/2026",
                              "effectiveDate": "2026-08-21",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.6839
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3122
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("162/A/NBP/2026");
        result.Value.PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value.Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value.Rates[0].Price.ShouldBe(3.6839m);
        result.Value.Rates[1].CurrencyName.ShouldBe("euro");
        result.Value.Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value.Rates[1].Price.ShouldBe(4.3122m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/a/last/{count}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "A",
                              "no": "160/A/NBP/2026",
                              "effectiveDate": "2026-08-19",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.7306
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3267
                                }
                              ]
                            },
                            {
                              "table": "A",
                              "no": "161/A/NBP/2026",
                              "effectiveDate": "2026-08-20",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.6896
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3165

                                }
                              ]
                            },
                            {
                              "table": "A",
                              "no": "162/A/NBP/2026",
                              "effectiveDate": "2026-08-21",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.6839
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3122
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetTopCountAsync(TableType.A, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(count);
        result.Value[0].TableId.ShouldBe("160/A/NBP/2026");
        result.Value[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value[0].Rates.Count.ShouldBe(2);
        result.Value[0].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[0].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[0].Rates[0].Price.ShouldBe(3.7306m);
        result.Value[0].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[0].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[0].Rates[1].Price.ShouldBe(4.3267m);
        result.Value[1].TableId.ShouldBe("161/A/NBP/2026");
        result.Value[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value[1].Rates.Count.ShouldBe(2);
        result.Value[1].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[1].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[1].Rates[0].Price.ShouldBe(3.6896m);
        result.Value[1].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[1].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[1].Rates[1].Price.ShouldBe(4.3165m);
        result.Value[2].TableId.ShouldBe("162/A/NBP/2026");
        result.Value[2].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value[2].Rates.Count.ShouldBe(2);
        result.Value[2].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[2].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[2].Rates[0].Price.ShouldBe(3.6839m);
        result.Value[2].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[2].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[2].Rates[1].Price.ShouldBe(4.3122m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var path = $"{BasePath}/a/today";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "A",
                              "no": "162/A/NBP/2026",
                              "effectiveDate": "2026-08-21",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.6839
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3122
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetTodayAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("162/A/NBP/2026");
        result.Value.PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value.Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value.Rates[0].Price.ShouldBe(3.6839m);
        result.Value.Rates[1].CurrencyName.ShouldBe("euro");
        result.Value.Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value.Rates[1].Price.ShouldBe(4.3122m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/a/{date:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                            "table": "A",
                            "no": "161/A/NBP/2026",
                            "effectiveDate": "2026-08-20",
                            "rates": [
                              {
                                "currency": "dolar amerykański",
                                "code": "USD",
                                "mid": 3.6896
                              },
                              {
                                "currency": "euro",
                                "code": "EUR",
                                "mid": 4.3165

                              }
                            ]
                          }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetAsync(TableType.A, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("161/A/NBP/2026");
        result.Value.PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value.Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value.Rates[0].Price.ShouldBe(3.6896m);
        result.Value.Rates[1].CurrencyName.ShouldBe("euro");
        result.Value.Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value.Rates[1].Price.ShouldBe(4.3165m);
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        var from = new DateOnly(2026, 8, 19);
        var to = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/a/{from:O}/{to:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                            {
                              "table": "A",
                              "no": "160/A/NBP/2026",
                              "effectiveDate": "2026-08-19",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.7306
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3267
                                }
                              ]
                            },
                            {
                              "table": "A",
                              "no": "161/A/NBP/2026",
                              "effectiveDate": "2026-08-20",
                              "rates": [
                                {
                                  "currency": "dolar amerykański",
                                  "code": "USD",
                                  "mid": 3.6896
                                },
                                {
                                  "currency": "euro",
                                  "code": "EUR",
                                  "mid": 4.3165
                                }
                              ]
                            }
                          ]
                          """));

        // Act
        var result = await CreateSut().GetAsync(TableType.A, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].TableId.ShouldBe("160/A/NBP/2026");
        result.Value[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value[0].Rates.Count.ShouldBe(2);
        result.Value[0].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[0].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[0].Rates[0].Price.ShouldBe(3.7306m);
        result.Value[0].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[0].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[0].Rates[1].Price.ShouldBe(4.3267m);
        result.Value[1].TableId.ShouldBe("161/A/NBP/2026");
        result.Value[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value[1].Rates.Count.ShouldBe(2);
        result.Value[1].Rates[0].CurrencyName.ShouldBe("dolar amerykański");
        result.Value[1].Rates[0].CurrencyCode.ShouldBe("USD");
        result.Value[1].Rates[0].Price.ShouldBe(3.6896m);
        result.Value[1].Rates[1].CurrencyName.ShouldBe("euro");
        result.Value[1].Rates[1].CurrencyCode.ShouldBe("EUR");
        result.Value[1].Rates[1].Price.ShouldBe(4.3165m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }
}
