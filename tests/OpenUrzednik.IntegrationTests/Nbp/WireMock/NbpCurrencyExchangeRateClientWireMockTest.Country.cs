using System.Net;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest
{
    [Fact]
    public async Task GetCountryLatestAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var path = $"{BasePath}/b/{currencyCode}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "B",
                            "country": "Ekwador",
                            "symbol": "618",
                            "currency": "dolar",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "10/B/NBP/2004",
                                "effectiveDate": "2004-04-28",
                                "mid": 4.011
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetCountryLatestAsync(currencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Country.ShouldBe("Ekwador");
        result.Value.CurrencySymbol.ShouldBe("618");
        result.Value.CurrencyName.ShouldBe("dolar");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("10/B/NBP/2004");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2004, 4, 28));
        result.Value.Rates[0].Price.ShouldBe(4.011m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetCountryTopCountAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        const int count = 3;
        var path = $"{BasePath}/b/{currencyCode}/last/{count}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "B",
                            "country": "Ekwador",
                            "symbol": "618",
                            "currency": "dolar",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "8/B/NBP/2004",
                                "effectiveDate": "2004-04-14",
                                "mid": 3.9785
                              },
                              {
                                "no": "9/B/NBP/2004",
                                "effectiveDate": "2004-04-21",
                                "mid": 4.0102
                              },
                              {
                                "no": "10/B/NBP/2004",
                                "effectiveDate": "2004-04-28",
                                "mid": 4.011
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetCountryTopCountAsync(currencyCode, count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Country.ShouldBe("Ekwador");
        result.Value.CurrencySymbol.ShouldBe("618");
        result.Value.CurrencyName.ShouldBe("dolar");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(count);
        result.Value.Rates[0].TableId.ShouldBe("8/B/NBP/2004");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2004, 4, 14));
        result.Value.Rates[0].Price.ShouldBe(3.9785m);
        result.Value.Rates[1].TableId.ShouldBe("9/B/NBP/2004");
        result.Value.Rates[1].PublicationDate.ShouldBe(new DateOnly(2004, 4, 21));
        result.Value.Rates[1].Price.ShouldBe(4.0102m);
        result.Value.Rates[2].TableId.ShouldBe("10/B/NBP/2004");
        result.Value.Rates[2].PublicationDate.ShouldBe(new DateOnly(2004, 4, 28));
        result.Value.Rates[2].Price.ShouldBe(4.011m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetCountryTodayAsync_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var path = $"{BasePath}/b/{currencyCode}/today";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "B",
                            "country": "Ekwador",
                            "symbol": "618",
                            "currency": "dolar",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "10/B/NBP/2004",
                                "effectiveDate": "2004-04-28",
                                "mid": 4.011
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetCountryTodayAsync(currencyCode, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Country.ShouldBe("Ekwador");
        result.Value.CurrencySymbol.ShouldBe("618");
        result.Value.CurrencyName.ShouldBe("dolar");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("10/B/NBP/2004");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2004, 4, 28));
        result.Value.Rates[0].Price.ShouldBe(4.011m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetCountryAsync_SpecificDate_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var date = new DateOnly(2004, 4, 21);
        var path = $"{BasePath}/b/{currencyCode}/{date:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "B",
                            "country": "Ekwador",
                            "symbol": "618",
                            "currency": "dolar",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "9/B/NBP/2004",
                                "effectiveDate": "2004-04-21",
                                "mid": 4.0102
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetCountryAsync(currencyCode, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Country.ShouldBe("Ekwador");
        result.Value.CurrencySymbol.ShouldBe("618");
        result.Value.CurrencyName.ShouldBe("dolar");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(1);
        result.Value.Rates[0].TableId.ShouldBe("9/B/NBP/2004");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2004, 4, 21));
        result.Value.Rates[0].Price.ShouldBe(4.0102m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetCountryAsync_DateRange_Returns200WithData_MapsToCurrencyData()
    {
        // Arrange
        const string currencyCode = "USD";
        var from = new DateOnly(2004, 4, 10);
        var to = new DateOnly(2004, 4, 21);
        var path = $"{BasePath}/b/{currencyCode}/{from:O}/{to:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                            "table": "B",
                            "country": "Ekwador",
                            "symbol": "618",
                            "currency": "dolar",
                            "code": "USD",
                            "rates": [
                              {
                                "no": "8/B/NBP/2004",
                                "effectiveDate": "2004-04-14",
                                "mid": 3.9785
                              },
                              {
                                "no": "9/B/NBP/2004",
                                "effectiveDate": "2004-04-21",
                                "mid": 4.0102
                              }
                            ]
                          }
                          """));

        // Act
        var result = await CreateSut().GetCountryAsync(currencyCode, from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Country.ShouldBe("Ekwador");
        result.Value.CurrencySymbol.ShouldBe("618");
        result.Value.CurrencyName.ShouldBe("dolar");
        result.Value.CurrencyCode.ShouldBe(currencyCode);
        result.Value.Rates.Count.ShouldBe(2);
        result.Value.Rates[0].TableId.ShouldBe("8/B/NBP/2004");
        result.Value.Rates[0].PublicationDate.ShouldBe(new DateOnly(2004, 4, 14));
        result.Value.Rates[0].Price.ShouldBe(3.9785m);
        result.Value.Rates[1].TableId.ShouldBe("9/B/NBP/2004");
        result.Value.Rates[1].PublicationDate.ShouldBe(new DateOnly(2004, 4, 21));
        result.Value.Rates[1].Price.ShouldBe(4.0102m);
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }
}
