# OpenUrzednik.Diagnostics

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Narodowym Bankiem Polskim (NBP) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Adaptery, które zamieniają spany klientów **OpenUrzednik.NET** na `Activity` z `System.Diagnostics` i zapisują ich metryki w `Meter`. Zbierają je OpenTelemetry i inne narzędzia.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/main/README.pl.md).

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

Na .NET 8 i nowszym to wystarczy: klient bez `TraceSource` sam zapisuje spany w źródle swojego dostawcy. Na .NET Framework (`netstandard2.0`) klient zapisuje spany tylko wtedy, gdy podasz mu `TraceSource` w `OpenUrzednikTelemetry`:

```csharp
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient, options: null, timeProvider: null, telemetry: new OpenUrzednikTelemetry
{
    TraceSource = ActivityTraceSource.GetShared(NbpTelemetry.SourceName),
});
```

## Metryki

Każda metoda klienta zapisuje czas swojego działania w `Meter` o tej samej nazwie co źródło dostawcy, `OpenUrzednik.<Provider>`. W OpenTelemetry włączysz wszystkie naraz:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("OpenUrzednik.*"));
```

Na .NET 8 i nowszym to wystarczy. Na .NET Framework klient zapisuje metryki tylko wtedy, gdy podasz mu `Metrics = MeterMetrics.GetShared(NbpTelemetry.SourceName)` w `OpenUrzednikTelemetry`. `AddOpenUrzednikNbp()` robi to za ciebie.

## Więcej po angielsku

- [Własne `ActivitySource`](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#usage)
- [Spany żądań `HttpClient`](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#http-client-spans)
- [Co trafia do śladu (spany, tagi, błędy, stabilność, .NET Framework)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#what-goes-into-the-trace)
- [Metryki (instrument, tagi, przedziały, stabilność)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#metrics)
- [Własny odbiornik metryk (`IOpenUrzednikMetrics`)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#your-own-metrics-sink)
- [Zależności](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#dependencies)
