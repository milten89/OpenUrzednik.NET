# OpenUrzednik.NET 💼🇵🇱

🇵🇱 [Wersja polska](README.md)

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Core?style=flat-square)](https://www.nuget.org/)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** is a set of open-source .NET libraries for Polish public APIs and public data. At this stage the project focuses on the shared foundations (`OpenUrzednik.Core`) and on the reference client for the NBP API (`OpenUrzednik.Nbp`), which the next packages will follow.

> ⚠️ **Unofficial project:** this is a community initiative. It is not affiliated with, authorized or sponsored by any Polish ministry or government office.

---

## 🚀 Why this project?

Most existing packages for Polish APIs are abandoned or don't follow modern .NET. OpenUrzednik.NET aims to provide:

- **A modern API:** support for `net8.0`, `net9.0` and `net10.0`, and through `netstandard2.0` also for .NET Framework (we test on .NET Framework 4.8)
- **One error model:** `OpenUrzednikResult` / `OpenUrzednikResult<T>` instead of error handling scattered across the code
- **Flexibility:** the classic exception style is still there through `.EnsureSuccess()` / `.EnsureSuccessAsync()`
- **A clear structure:** shared code in `Core`, one package per provider

---

## 📦 Packages

| Package | Status | Description | Targets |
| :--- | :--- | :--- | :--- |
| [**OpenUrzednik.Core**](src/OpenUrzednik.Core/README.en.md) | ✅ Core abstractions available | `OpenUrzednikResult`, `OpenUrzednikResult<T>`, `OpenUrzednikError`, `OpenUrzednikException` and the `EnsureSuccess` / `EnsureSuccessAsync` extension methods | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Http**](src/OpenUrzednik.Http/README.en.md) | 🧪 Preview | Shared HTTP layer for REST/JSON providers (`RestRequestExecutor`). Used by provider packages, not by apps | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Extensions.Logging**](src/OpenUrzednik.Extensions.Logging/README.en.md) | 🧪 Preview | Sends the clients' logs to `ILogger` from Microsoft.Extensions.Logging | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Diagnostics**](src/OpenUrzednik.Diagnostics/README.en.md) | 🧪 Preview | Turns the clients' spans into `Activity` objects for OpenTelemetry (`AddSource("OpenUrzednik.*")`) | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Nbp**](src/OpenUrzednik.Nbp/README.en.md) | 🧪 Preview | Exchange rates (tables A, B, C), rate tables and gold prices from the NBP API. The public API may still change | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Nbp.DependencyInjection**](src/OpenUrzednik.Nbp.DependencyInjection/README.en.md) | 🧪 Preview | `AddOpenUrzednikNbp()`: the NBP clients in DI with `IHttpClientFactory`, ready for `AddStandardResilienceHandler()` | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.en.md) | 🚧 Skeleton, not published | Placeholder for the GUS integration | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.en.md) | 🚧 Skeleton, not published | Placeholder for the KRS integration | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.en.md) | 🚧 Skeleton, not published | Placeholder for the VAT white list (Biała Lista) integration | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |

---

## 🛠️ Quick start

The easiest place to start right now is `OpenUrzednik.Core`.

### Using `OpenUrzednikResult`

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;

OpenUrzednikResult<int> result = OpenUrzednikResult.Success(42);
int value = result.EnsureSuccess();
Console.WriteLine(value);
```

A business error:

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;

OpenUrzednikResult<int> failed = OpenUrzednikResult.Failure<int>(new ValidationError("NIP is invalid."));

try
{
    int value = failed.EnsureSuccess();
}
catch (ValidationException ex)
{
    Console.WriteLine(ex.Message);
}
```

### Clients and DI

The `OpenUrzednik.Nbp` package contains `NbpCurrencyExchangeRateClient`, `NbpExchangeRateTableClient` and `NbpGoldPriceClient` (preview). With a DI container, register them with [OpenUrzednik.Nbp.DependencyInjection](src/OpenUrzednik.Nbp.DependencyInjection/README.en.md). `OpenUrzednik.Gus`, `OpenUrzednik.Krs` and `OpenUrzednik.Mf` are skeletons and aren't on NuGet; work on them starts after the NBP package is finished ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)).

### Samples

You'll find runnable programs using the NBP clients, with and without a DI container, in [`samples/`](samples/README.en.md).

### Tests

You'll find the tests in `tests/`: unit tests, HTTP tests with WireMock, and tests against the real API (optional, run on request).

---

## 🤝 Contributing

Want to add another data source or report a bug? See [CONTRIBUTING.en.md](CONTRIBUTING.en.md) for how we work on the repository, the API conventions and the pull request steps.

## 📄 License

The project is available under the MIT license. See the `LICENSE` file.
