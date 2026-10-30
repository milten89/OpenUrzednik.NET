# OpenUrzednik.Diagnostics

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Narodowym Bankiem Polskim (NBP) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Adapter, który zamienia spany klientów **OpenUrzednik.NET** na `Activity` z `System.Diagnostics`. Zbierają je OpenTelemetry i inne narzędzia do śledzenia.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.Diagnostics
```

## Użycie

Źródła nazywają się `OpenUrzednik.<Provider>` (np. `OpenUrzednik.Nbp`). W OpenTelemetry włączysz wszystkie naraz:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("OpenUrzednik.*"));
```

Na .NET 8 i nowszym to wystarczy: klient bez `traceSource` sam zapisuje spany w źródle swojego dostawcy. Na .NET Framework (`netstandard2.0`) klient zapisuje spany tylko wtedy, gdy podasz mu `traceSource`:

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient,
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

## Więcej po angielsku

- [Własne `ActivitySource`](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#usage)
- [Spany żądań `HttpClient`](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#http-client-spans)
- [Co trafia do śladu (spany, tagi, błędy, stabilność, .NET Framework)](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#what-goes-into-the-trace)
- [Zależności](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md#dependencies)
