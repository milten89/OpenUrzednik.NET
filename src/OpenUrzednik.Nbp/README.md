# OpenUrzednik.Nbp

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by Narodowy Bank Polski (NBP) or any other Polish public institution.

**NBP** (Narodowy Bank Polski, the National Bank of Poland) client for OpenUrzednik.NET: exchange rates and gold prices from the NBP API.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Nbp
```

Requires [OpenUrzednik.Core](https://www.nuget.org/packages/OpenUrzednik.Core) (NuGet installs it as a dependency).

## Usage

The simplest case: the default NBP API address and the `HttpClient`'s timeout.

```csharp
using var httpClient = new HttpClient();
var client = new NbpGoldPriceClient(httpClient);

var result = await client.GetLatestAsync();
if (result.IsSuccess)
    Console.WriteLine($"{result.Value.Date:yyyy-MM-dd}: {result.Value.Price} zł");
else
    Console.WriteLine(result.Errors[0].Message);
```

Pass your own address (e.g. a proxy or an API gateway) and timeout in `NbpOptions`. The client doesn't change the `HttpClient`, so you can share it with other code:

```csharp
var client = new NbpCurrencyExchangeRateClient(httpClient, new NbpOptions
{
    ApiUrl = "https://proxy.example.com/nbp/",
    Timeout = TimeSpan.FromSeconds(5),
});
```

With a DI container, use [OpenUrzednik.Nbp.DependencyInjection](https://www.nuget.org/packages/OpenUrzednik.Nbp.DependencyInjection): `builder.Services.AddOpenUrzednikNbp()` registers all three clients, and `.AddStandardResilienceHandler()` adds retries.

The client takes the API address from the first place that sets one: `NbpOptions.ApiUrl`, `HttpClient.BaseAddress`, and finally the default `https://api.nbp.pl/api/`. If you don't set `NbpOptions.Timeout`, the `HttpClient`'s timeout applies (100 s by default). `NbpOptions.Timeout` can shorten that limit but not extend it, because `HttpClient.Timeout` still applies until the response headers arrive. The API address must be an absolute `https` URL without a query (`?…`) or a fragment (`#…`): the constructor throws an `ArgumentException` for any other scheme, for `NbpOptions.ApiUrl` and for `HttpClient.BaseAddress` alike.

Errors such as missing data, an exceeded rate limit, a timeout or an invalid currency code come back in the result, not as exceptions. If you prefer exceptions, call `EnsureSuccess()` on the result. A `null` currency code or an undefined `TableType` is a programming error: the method throws `ArgumentNullException` or `ArgumentOutOfRangeException` right away, before it returns a task. A resilience handler's rejections (timeout, open circuit breaker, rate limiter) become errors only when the clients are registered with `AddOpenUrzednikNbp()`; with your own `HttpClient` setup they come out as Polly exceptions.

## Methods

Each client has the same groups of methods; every method takes a `CancellationToken` last. `TableType` is `A` (common currencies, the default for rates) or `B` (less common currencies); table C (buy and sell rates) has its own `GetBuySell…` methods.

| Group | `NbpCurrencyExchangeRateClient` | `NbpExchangeRateTableClient` | `NbpGoldPriceClient` |
|---|---|---|---|
| Latest | `GetLatestAsync(currency, table)`, `GetBuySellLatestAsync(currency)` | `GetLatestAsync(table)`, `GetBuySellLatestAsync()` | `GetLatestAsync()` |
| Today | `GetTodayAsync(currency, table)`, `GetBuySellTodayAsync(currency)` | `GetTodayAsync(table)`, `GetBuySellTodayAsync()` | `GetTodayAsync()` |
| One date | `GetAsync(currency, date, table)`, `GetBuySellAsync(currency, date)` | `GetAsync(table, date)`, `GetBuySellAsync(date)` | `GetAsync(date)` |
| Date range | `GetAsync(currency, from, to, table)`, `GetBuySellAsync(currency, from, to)` | `GetAsync(table, from, to)`, `GetBuySellAsync(from, to)` | `GetAsync(from, to)` |
| Last N | `GetTopCountAsync(currency, topCount, table)`, `GetBuySellTopCountAsync(currency, topCount)` | `GetTopCountAsync(table, topCount)`, `GetBuySellTopCountAsync(topCount)` | `GetTopCountAsync(topCount)` |

The currency client returns one currency's rates (`CurrencyExchangeRates`, with a `Rates` list), the table client whole tables (`ExchangeRateTable`) and the gold client gold prices (`GoldPrice`). A range keeps the order NBP returns, which today is ascending by date.

## Data availability and limits

**Publication.** Times are Europe/Warsaw time.

- Table A: business days, between 11:45 and 12:15.
- Table B: Wednesdays, between 11:45 and 12:15, or the previous business day when Wednesday is a holiday.
- Table C (buy and sell): business days, between 7:45 and 8:15.
- Gold prices: business days, at about 8:00. NBP doesn't document this hour. We observed it on the API.

`Get…TodayAsync` returns a `NotFoundError` until the day's publication, and on days without one. `GetLatestAsync` returns the most recent published value.

**Limits.** The client checks these before it sends a request; a violation comes back as a `ValidationError`.

| What | Limit |
|---|---|
| Earliest date | 2002-01-02 for rates and tables, 2013-01-02 for gold |
| Latest date | Today's date in Europe/Warsaw, not in the caller's time zone or UTC |
| Top count | 1 to 255 for rates and gold; for tables, 1 to 67 for A and C, 1 to 14 for B |
| Date range | `to - from` at most 367 days for rates and gold, 93 days for tables |
| Currency code | 3 letters, upper or lower case |

In a range method, `from` may be earlier than the earliest date: only `to` is checked against the earliest date and against today. `from` later than `to` is a validation error.

**Empty answers.** A date or range with no publication (a weekend, a holiday, a date before the table existed) is a `NotFoundError`: NBP answers it with an HTTP 404 (the error then carries the status code) or with an empty list. See [Errors](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md#errors) for the other error types. NBP describes its API in the [official documentation](https://api.nbp.pl/en.html).

## The last rate before a date

To get the rate of the last publication before a given date, request a short range that ends the day before and take the item with the latest `PublicationDate`. Weekends and holidays have no publication, so the range has to reach back far enough: 10 days for tables A and C, 21 days for table B (published weekly).

```csharp
var date = new DateTime(2026, 3, 16);

var result = await client.GetAsync("USD", date.AddDays(-10), date.AddDays(-1));
if (result.IsSuccess)
{
    var rate = result.Value.Rates[0];
    foreach (var item in result.Value.Rates)
    {
        if (item.PublicationDate > rate.PublicationDate)
            rate = item;
    }

    Console.WriteLine($"{rate.PublicationDate:yyyy-MM-dd}: {rate.Price}");
}
else
{
    Console.WriteLine(result.Errors[0].Message);
}
```

For table B, pass `TableType.B` and `date.AddDays(-21)`. For buy and sell rates, use `GetBuySellAsync(currency, from, to)`; its items have `Ask` and `Bid`. A range without any publication is a `NotFoundError`.

## HttpClient and thread safety

The clients don't own the `HttpClient`, never change it and aren't `IDisposable`. Create one `HttpClient` for the application, or take it from `IHttpClientFactory` (the [DependencyInjection package](https://www.nuget.org/packages/OpenUrzednik.Nbp.DependencyInjection) does that), and share it. Don't create an `HttpClient` per request: it exhausts sockets.

The clients are thread-safe: create one client per `HttpClient` and share it across threads and concurrent calls.

## Compatibility

Minor versions may add methods to the client interfaces, properties to the models and options to `NbpOptions` ([ADR-0011](https://github.com/milten89/OpenUrzednik.NET/blob/develop/docs/adr/0011-compatibility-policy.md)). The clients and `NbpOptions` are `sealed`: to swap or wrap a client, use its interface (`INbpGoldPriceClient` and the others). If you implement an interface yourself, a new method breaks your build until you add it; a `throw new NotImplementedException();` stub is enough for methods you don't call. An implementation compiled against an older version, e.g. in another package, has to be rebuilt. New model properties are `init` properties (set them in an object initializer or with `with`); the positional constructors and `Deconstruct` don't change.

## API gateways

For most gateways in front of the NBP API, `NbpOptions.ApiUrl` is enough: the client appends the NBP paths (e.g. `exchangerates/rates/a/USD`) to it. If the gateway renames paths or needs a query parameter, rewrite the request in a `DelegatingHandler`. The paths the client builds aren't configurable in any other way.

```csharp
sealed class NbpGatewayHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = new UriBuilder(request.RequestUri!);
        uri.Path = uri.Path.Replace("/exchangerates/", "/fx/");
        uri.Query = string.IsNullOrEmpty(uri.Query) ? "api-version=1" : uri.Query.TrimStart('?') + "&api-version=1";
        request.RequestUri = uri.Uri;
        return base.SendAsync(request, cancellationToken);
    }
}
```

```csharp
using var httpClient = new HttpClient(new NbpGatewayHandler { InnerHandler = new HttpClientHandler() });
var client = new NbpCurrencyExchangeRateClient(httpClient, new NbpOptions { ApiUrl = "https://gateway.example.com/nbp/" });
// GetLatestAsync("USD") requests https://gateway.example.com/nbp/fx/rates/a/USD?api-version=1
```

With `AddOpenUrzednikNbp()`, add the handler with `.AddHttpMessageHandler(() => new NbpGatewayHandler())`. Spans still show the NBP path (`url.path`), because the client tags it before the handler runs; error messages show the rewritten address.

## Dates and .NET Framework

Dates are `DateTime` on every target: methods take a `DateTime` and the models return one (e.g. `GoldPrice.Date`). Only the date counts: the client ignores the time of day and `DateTime.Kind`, and returned dates have the time `00:00` and `DateTimeKind.Unspecified`. On .NET 8 and later you can also pass a `DateOnly` (extension methods in `OpenUrzednik.Nbp`).

The package also runs on .NET Framework 4.7.2 or later (through `netstandard2.0`) with the same API, so a `netstandard2.0` library that uses it works in .NET Framework and .NET apps alike.

## Logging and tracing

The clients take optional `logger` and `traceSource` parameters. Without a logger they log nothing. Without a trace source, on .NET 8 and later they record spans on the `OpenUrzednik.Nbp` `ActivitySource` (`NbpTelemetry.SourceName`), which costs nothing while no one listens to it; on .NET Framework they record none. To use `ILogger` and OpenTelemetry, install the [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging) and [OpenUrzednik.Diagnostics](https://www.nuget.org/packages/OpenUrzednik.Diagnostics) adapters:

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient,
    logger: loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>(),
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

On .NET 8 and later the `traceSource` line is optional; to turn spans off, pass `NullOpenUrzednikTraceSource.Instance`. Subscribe with `AddSource("OpenUrzednik.*")` or `AddSource(NbpTelemetry.SourceName)`. Logs go to a category named after the client's full type name (e.g. `OpenUrzednik.Nbp.NbpGoldPriceClient`).

The clients use their own interfaces (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` from OpenUrzednik.Core), not `ILogger` and `ActivitySource`. So this package needs neither `Microsoft.Extensions.Logging.Abstractions` nor any other logging or tracing package. Only the adapters connect it to `ILogger` and `ActivitySource`.

## Telemetry reference

Within a major version, the names below don't change: the source, span names, tags, `error.type` values, logger categories and event ids. Minor versions may add spans, tags and events. Log message text and properties may change in any version, so filter logs by event id, not by text.

### Spans

Each client method starts one span on the `OpenUrzednik.Nbp` source, with the HTTP request as its child `nbp.http.get`. All spans are of kind `Internal`.

| Span | Client and methods | Tags |
|---|---|---|
| `nbp.currency.latest`, `.today` | `NbpCurrencyExchangeRateClient.GetLatestAsync`, `GetTodayAsync` | `nbp.currency`, `nbp.table` |
| `nbp.currency.top_count` | `GetTopCountAsync` | `nbp.currency`, `nbp.table`, `nbp.top_count` |
| `nbp.currency.date` | `GetAsync(currency, date)` | `nbp.currency`, `nbp.table`, `nbp.date` |
| `nbp.currency.range` | `GetAsync(currency, from, to)` | `nbp.currency`, `nbp.table`, `nbp.from`, `nbp.to` |
| `nbp.currency.buy_sell_latest`, `.buy_sell_today` | `GetBuySellLatestAsync`, `GetBuySellTodayAsync` | `nbp.currency` |
| `nbp.currency.buy_sell_top_count` | `GetBuySellTopCountAsync` | `nbp.currency`, `nbp.top_count` |
| `nbp.currency.buy_sell_date` | `GetBuySellAsync(currency, date)` | `nbp.currency`, `nbp.date` |
| `nbp.currency.buy_sell_range` | `GetBuySellAsync(currency, from, to)` | `nbp.currency`, `nbp.from`, `nbp.to` |
| `nbp.table.latest`, `.today` | `NbpExchangeRateTableClient.GetLatestAsync`, `GetTodayAsync` | `nbp.table` |
| `nbp.table.top_count` | `GetTopCountAsync` | `nbp.table`, `nbp.top_count` |
| `nbp.table.date` | `GetAsync(table, date)` | `nbp.table`, `nbp.date` |
| `nbp.table.range` | `GetAsync(table, from, to)` | `nbp.table`, `nbp.from`, `nbp.to` |
| `nbp.table.buy_sell_latest`, `.buy_sell_today` | `GetBuySellLatestAsync`, `GetBuySellTodayAsync` | none |
| `nbp.table.buy_sell_top_count` | `GetBuySellTopCountAsync` | `nbp.top_count` |
| `nbp.table.buy_sell_date` | `GetBuySellAsync(date)` | `nbp.date` |
| `nbp.table.buy_sell_range` | `GetBuySellAsync(from, to)` | `nbp.from`, `nbp.to` |
| `nbp.gold.latest`, `.today` | `NbpGoldPriceClient.GetLatestAsync`, `GetTodayAsync` | none |
| `nbp.gold.top_count` | `GetTopCountAsync` | `nbp.top_count` |
| `nbp.gold.date` | `GetAsync(date)` | `nbp.date` |
| `nbp.gold.range` | `GetAsync(from, to)` | `nbp.from`, `nbp.to` |
| `nbp.http.get` | the request of any method | `http.request.method`, `url.path` (e.g. `/api/cenyzlota`), `http.response.status_code` |

Tag values: `nbp.currency` is the code as passed (e.g. `USD`), `nbp.table` the table (`A` or `B`), `nbp.top_count` a number, and `nbp.date`, `nbp.from` and `nbp.to` dates as `yyyy-MM-dd`. A failed span has the `Error` status and an `error.type` tag with the error code (see [Errors](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md#errors)).

### Log events

Every entry has an event id; `NbpLogEvents` lists them as constants, and the event name is the constant's name (`EventId(1001, "RequestTimedOut")`). The ids are the same in every client. The `{provider}` property is `nbp`.

| Id | Name | Level | When |
|---|---|---|---|
| 1001 | `RequestTimedOut` | Warning | A request timed out (`NbpOptions.Timeout`, `HttpClient.Timeout` or a resilience handler's timeout). |
| 1002 | `ConnectionFailed` | Warning | The API couldn't be reached (DNS, TLS, connection) or the body read broke. |
| 1003 | `RequestRejected` | Warning | A resilience handler rejected the request (open circuit breaker, rate limiter). |
| 1101 | `NotFound` | Debug | The API returned 404. |
| 1102 | `BadRequest` | Warning | The API returned 400. |
| 1103 | `Unauthorized` | Warning | The API returned 401 or 403. |
| 1104 | `RequestTimeoutStatus` | Warning | The API returned 408. |
| 1105 | `RateLimited` | Warning | The API returned 429. |
| 1106 | `ServiceUnavailable` | Warning | The API returned a 5xx status. |
| 1107 | `UnexpectedStatus` | Warning | The API returned another non-success status. |
| 1108 | `ErrorResponse` | Warning | Reserved for providers that map error bodies themselves; NBP doesn't. |
| 1109 | `ErrorBodyUnreadable` | Debug | The body of an error response couldn't be read. |
| 1201 | `EmptyResponse` | Error | A success response had no payload. |
| 1202 | `DeserializationFailed` | Error | A success response wasn't the expected JSON. |
| 1203 | `DecodingFailed` | Error | A success response declared a charset that can't be decoded. |
| 2001 | `ValidationFailed` | Debug | The arguments failed validation; no request was sent. |
| 2002 | `NoData` | Debug | The API returned an empty array: no data for the date or range. |
| 2003 | `MappingFailed` | Error | A response lacked data the model needs. |
