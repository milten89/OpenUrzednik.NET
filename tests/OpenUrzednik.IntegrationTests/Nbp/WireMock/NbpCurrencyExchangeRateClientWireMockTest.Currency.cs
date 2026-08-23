using System.Net;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var path = $"{BasePath}/a/{currencyCode}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "A",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "162/A/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "mid": 3.6839
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetLatestAsync(currencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("162/A/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[0].Price.ShouldBe(3.6839m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        const int count = 3;
        var path = $"{BasePath}/a/{currencyCode}/last/{count}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "A",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "160/A/NBP/2026",
                                "effectiveDate": "2026-08-19",
                                "mid": 3.7306
                              },
                              {
                                "no": "161/A/NBP/2026",
                                "effectiveDate": "2026-08-20",
                                "mid": 3.6896
                              },
                              {
                                "no": "162/A/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "mid": 3.6839
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetTopCountAsync(currencyCode, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(count);
        result.Value.Rates[0].TableId.ShouldBe("160/A/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value.Rates[0].Price.ShouldBe(3.7306m);
        result.Value.Rates[1].TableId.ShouldBe("161/A/NBP/2026");
        result.Value.Rates[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates[1].Price.ShouldBe(3.6896m);
        result.Value.Rates[2].TableId.ShouldBe("162/A/NBP/2026");
        result.Value.Rates[2].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[2].Price.ShouldBe(3.6839m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var path = $"{BasePath}/a/{currencyCode}/today";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "A",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "162/A/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "mid": 3.6839
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetTodayAsync(currencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("162/A/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[0].Price.ShouldBe(3.6839m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var date = new DateOnly(2004, 4, 21);
        var path = $"{BasePath}/a/{currencyCode}/{date:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "A",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "162/A/NBP/2026",
                                "effectiveDate": "2026-08-21",
                                "mid": 3.6839
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetAsync(currencyCode, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("162/A/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 21));
        result.Value.Rates[0].Price.ShouldBe(3.6839m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var from = new DateOnly(2004, 4, 10);
        var to = new DateOnly(2004, 4, 21);
        var path = $"{BasePath}/a/{currencyCode}/{from:O}/{to:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "A",
                            "currency": "dolar amerykański",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "160/A/NBP/2026",
                                "effectiveDate": "2026-08-19",
                                "mid": 3.7306
                              },
                              {
                                "no": "161/A/NBP/2026",
                                "effectiveDate": "2026-08-20",
                                "mid": 3.6896
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetAsync(currencyCode, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CurrencyName.ShouldBe("dolar amerykański");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].TableId.ShouldBe("160/A/NBP/2026");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2026, 8, 19));
        result.Value.Rates[0].Price.ShouldBe(3.7306m);
        result.Value.Rates[1].TableId.ShouldBe("161/A/NBP/2026");
        result.Value.Rates[1].PublicationDate.ShouldBe(new DateOnly(2026, 8, 20));
        result.Value.Rates[1].Price.ShouldBe(3.6896m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }
}
