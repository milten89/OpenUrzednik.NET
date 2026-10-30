<h1 align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="assets/branding/header-dark.svg">
    <img alt="OpenUrzędnik.NET" src="assets/branding/header-light.svg" width="483">
  </picture>
</h1>

🇵🇱 [Wersja polska (skrócona)](README.pl.md)

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Nbp?style=flat-square)](https://www.nuget.org/packages/OpenUrzednik.Nbp)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** is a set of open-source .NET libraries for Polish public APIs and public data. At this stage the project focuses on the shared foundations (`OpenUrzednik.Core`) and on the reference client for the NBP API (`OpenUrzednik.Nbp`), which the next packages will follow.

> ⚠️ **Unofficial project:** this is a community initiative. It is not affiliated with, authorized or endorsed by Narodowy Bank Polski (NBP), any ministry, government office or any other Polish public institution.

---

## 🚀 Why this project?

Most existing packages for Polish APIs are abandoned or don't follow modern .NET. OpenUrzednik.NET aims to provide:

- **A modern API:** builds for the .NET LTS releases, and through `netstandard2.0` also for .NET Framework 4.7.2 or later (see [Supported platforms](#supported-platforms))
- **One error model:** `OpenUrzednikResult` / `OpenUrzednikResult<T>` instead of error handling scattered across the code
- **Flexibility:** the classic exception style is still there through `.EnsureSuccess()` / `.EnsureSuccessAsync()`
- **A clear structure:** shared code in `Core`, one package per provider

---

## 📦 Packages

| Package | Status | Description | Targets |
| :--- | :--- | :--- | :--- |
| [**OpenUrzednik.Core**](src/OpenUrzednik.Core/README.md) | ✅ Core abstractions available | `OpenUrzednikResult`, `OpenUrzednikResult<T>`, `OpenUrzednikError`, `OpenUrzednikException` and `EnsureSuccess()` / `EnsureSuccessAsync()` | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Extensions.Logging**](src/OpenUrzednik.Extensions.Logging/README.md) | 🧪 Preview | Sends the clients' logs to `ILogger` from Microsoft.Extensions.Logging | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Diagnostics**](src/OpenUrzednik.Diagnostics/README.md) | 🧪 Preview | Turns the clients' spans into `Activity` objects for OpenTelemetry (`AddSource("OpenUrzednik.*")`) | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Nbp**](src/OpenUrzednik.Nbp/README.md) | 🧪 Preview | Exchange rates (tables A, B, C), rate tables and gold prices from the NBP API. The public API may still change | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Nbp.DependencyInjection**](src/OpenUrzednik.Nbp.DependencyInjection/README.md) | 🧪 Preview | `AddOpenUrzednikNbp()`: the NBP clients in DI with `IHttpClientFactory`, ready for `AddStandardResilienceHandler()` | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.md) | 🚧 Planned, not started | GUS client (company lookups by REGON/NIP) | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.md) | 🚧 Planned, not started | KRS client (the National Court Register) | `netstandard2.0`, `net8.0`, `net10.0` |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.md) | 🚧 Planned, not started | VAT white list (Biała Lista) client | `netstandard2.0`, `net8.0`, `net10.0` |

## Supported platforms

| Your app | Build it uses |
| :--- | :--- |
| .NET 10 | `net10.0` |
| .NET 8 and .NET 9 | `net8.0` (the same API) |
| .NET Framework 4.7.2 or later | `netstandard2.0` (the same API, without the `DateOnly` overloads) |

- We build for `netstandard2.0` and the .NET LTS releases that Microsoft supports (and a newer release only when we need its APIs). CI tests every build; the `netstandard2.0` build runs its tests on .NET Framework 4.8.
- .NET 8 and .NET 9 reach the end of Microsoft's support on 10 November 2026. Your .NET 8 or 9 app keeps working with the `net8.0` build; we'll remove it only in a major version, because it is our lowest .NET target.
- We may remove a .NET target that isn't the lowest one in a minor version, after Microsoft ends its support; apps on that version then get the next lower build, with the same API. `netstandard2.0` stays, so .NET Framework keeps working.
- The assemblies are strong-named (public key token `e8d5ca0d09eb5ce1`), so strong-named .NET Framework apps and libraries can use them. The assembly version changes only with the major version (`1.0.0.0` for every 1.x release), so binding redirects stay valid across minor and patch updates.
- We don't support .NET Framework 4.6.1 to 4.7.1: there, `netstandard2.0` libraries need extra facade assemblies or binding redirects (Microsoft recommends 4.7.2 or later).

## Versioning and support

The packages share one version and follow [Semantic Versioning](https://semver.org/). From 1.0, a minor or patch version doesn't break the public API on any target, documented behaviour, error types and codes (`notFound`, `rateLimitExceeded`, …) or telemetry names (sources, spans, tags, log event ids).

A minor version may add client methods, model properties and options, raise a dependency's minimum version, or drop a .NET target that isn't the lowest one once Microsoft has ended its support. A patch version may raise a dependency's patch version to fix a vulnerability. Log and exception message text can change in any version, and previews may break anything. The full rules are in [ADR-0011](docs/adr/0011-compatibility-policy.md).

Fixes go to the latest minor version of the latest major version, as a new patch version: to get a fix, update to the latest minor. Previews aren't supported. When a new major version is released, the previous major version still gets security fixes for 6 months. To report a vulnerability, see [SECURITY.md](SECURITY.md).

---

## 🛠️ Quick start

Install the NBP client:

```bash
dotnet add package OpenUrzednik.Nbp
```

Until 1.0.0, add `--prerelease`: only previews are published so far.

```csharp
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Gold;

// One HttpClient for the whole app; the clients don't change it.
using var httpClient = new HttpClient();
var gold = new NbpGoldPriceClient(httpClient);
var rates = new NbpCurrencyExchangeRateClient(httpClient);

// Expected failures (no data, timeouts, HTTP errors) come back as errors, not exceptions.
var latest = await gold.GetLatestAsync();
if (latest.IsSuccess)
    Console.WriteLine($"Gold, 1 g: {latest.Value.Price} PLN ({latest.Value.Date:yyyy-MM-dd})");
else
    Console.WriteLine($"Gold price unavailable: {latest.Errors[0].Message}");

// Match handles both cases in one expression.
var usd = await rates.GetTopCountAsync("USD", 5);
Console.WriteLine(usd.Match(
    value => $"USD, last {value.Rates.Count} mid rates: {string.Join(", ", value.Rates.Select(r => r.Price))}",
    errors => $"USD rates unavailable: {errors[0].Message}"));

// Prefer exceptions? EnsureSuccess() throws an OpenUrzednikException subtype for a failed result.
try
{
    GoldPrice price = (await gold.GetAsync(new DateTime(2025, 12, 25))).EnsureSuccess(); // no price on Christmas
    Console.WriteLine(price.Price);
}
catch (OpenUrzednikException ex)
{
    Console.WriteLine($"{ex.GetType().Name} ({ex.Error?.Code}): {ex.Message}");
}
```

Every call returns an `OpenUrzednikResult<T>`. [Working with a result](src/OpenUrzednik.Core/README.md#working-with-a-result) covers `Map`, `Bind`, `TryGetValue` and the exceptions; the [OpenUrzednik.Nbp README](src/OpenUrzednik.Nbp/README.md) lists the other methods and the API's limits.

### Clients and DI

The `OpenUrzednik.Nbp` package contains `NbpCurrencyExchangeRateClient`, `NbpExchangeRateTableClient` and `NbpGoldPriceClient` (preview). With a DI container, register them with [OpenUrzednik.Nbp.DependencyInjection](src/OpenUrzednik.Nbp.DependencyInjection/README.md). `OpenUrzednik.Gus`, `OpenUrzednik.Krs` and `OpenUrzednik.Mf` are planned but not started: work on them begins after the first stable release of `OpenUrzednik.Nbp` ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)). We deprecated and unlisted their early empty previews on nuget.org.

### Samples

You'll find runnable programs using the NBP clients, with and without a DI container, in [`samples/`](samples/README.md).

### Tests

You'll find the tests in `tests/`: unit tests, HTTP tests with WireMock, and tests against the real API (optional, run on request).

---

## 🤝 Contributing

Found a bug, or want another data source? Open an issue with one of the [issue forms](https://github.com/milten89/OpenUrzednik.NET/issues/new/choose); a new data source goes through the provider proposal form. See [CONTRIBUTING.md](CONTRIBUTING.md) for how we work on the repository, the API conventions and the pull request steps.

## 📄 License

The project is available under the MIT license. See the `LICENSE` file.
