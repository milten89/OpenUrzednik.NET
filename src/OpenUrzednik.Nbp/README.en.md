# OpenUrzednik.Nbp

🇵🇱 [Wersja polska](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md)

**NBP** (Narodowy Bank Polski, the National Bank of Poland) client for OpenUrzednik.NET: exchange rates and gold prices from the NBP API.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.en.md) for an overview of the project.

## Status

🧪 Preview: clients for exchange rates (`NbpCurrencyExchangeRateClient`), rate tables (`NbpExchangeRateTableClient`) and gold prices (`NbpGoldPriceClient`) are available. The public API may still change before the stable release.

## Installation

```bash
dotnet add package OpenUrzednik.Nbp
```

Requires [OpenUrzednik.Core](https://www.nuget.org/packages/OpenUrzednik.Core) and [OpenUrzednik.Http](https://www.nuget.org/packages/OpenUrzednik.Http) (NuGet installs them as dependencies).

## Usage

The simplest case: the default NBP API address and the `HttpClient`'s timeout.

```csharp
using var httpClient = new HttpClient();
var client = new NbpGoldPriceClient(httpClient);

var result = await client.GetLatestAsync();
if (result.IsSuccess)
    Console.WriteLine($"{result.Value.Date}: {result.Value.Price} zł");
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

The client takes the API address from the first place that sets one: `NbpOptions.ApiUrl`, `HttpClient.BaseAddress`, and finally the default `https://api.nbp.pl/api/`. If you don't set `NbpOptions.Timeout`, the `HttpClient`'s timeout applies (100 s by default). `NbpOptions.Timeout` can shorten that limit but not extend it, because `HttpClient.Timeout` still applies until the response headers arrive. The API address can't contain a query (`?…`) or a fragment (`#…`).

Errors such as missing data, an exceeded rate limit or a timeout come back in the result, not as exceptions. If you prefer exceptions, call `EnsureSuccess()` on the result.

## .NET Framework

The package also runs on .NET Framework (through `netstandard2.0`). There, dates are `DateTime`, not `DateOnly`: methods take a `DateTime` and the models return one (e.g. `GoldPrice.Date`). Only the date counts: the client ignores the time of day, and returned dates have the time `00:00` and `DateTimeKind.Unspecified`.

If you write a library that uses this package, build it for the same targets (e.g. `netstandard2.0;net8.0`). A `netstandard2.0`-only library calls the `DateTime` methods, but in a .NET 8+ app NuGet picks the `DateOnly` build, so you get a `MissingMethodException`.

## Logging and tracing

The clients take optional `logger` and `traceSource` parameters. Without them they log nothing and create no spans. To use `ILogger` and OpenTelemetry, install the [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging) and [OpenUrzednik.Diagnostics](https://www.nuget.org/packages/OpenUrzednik.Diagnostics) adapters:

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Gold;

var client = new NbpGoldPriceClient(httpClient,
    logger: loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>(),
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

Spans go to the `OpenUrzednik.Nbp` source, and logs to a category named after the client's full type name (e.g. `OpenUrzednik.Nbp.Gold.NbpGoldPriceClient`).

The clients use their own interfaces (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` from OpenUrzednik.Core), not `ILogger` and `ActivitySource`. So this package needs neither `Microsoft.Extensions.Logging.Abstractions` nor any other logging or tracing package. Only the adapters connect it to `ILogger` and `ActivitySource`.
