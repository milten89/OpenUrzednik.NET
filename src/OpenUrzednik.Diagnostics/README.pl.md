# OpenUrzednik.Diagnostics

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

Adapter, który zamienia spany klientów **OpenUrzednik.NET** na `Activity` z `System.Diagnostics`. Zbierają je OpenTelemetry i inne narzędzia do śledzenia.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.Diagnostics
```

## Użycie

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient,
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

Źródła nazywają się `OpenUrzednik.<Provider>` (np. `OpenUrzednik.Nbp`). W OpenTelemetry włączysz wszystkie naraz:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("OpenUrzednik.*"));
```

## Więcej po angielsku

- [Własne `ActivitySource`](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#usage)
- [Co trafia do śladu (spany, tagi, błędy, .NET Framework)](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#what-goes-into-the-trace)
- [Zależności](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#dependencies)
