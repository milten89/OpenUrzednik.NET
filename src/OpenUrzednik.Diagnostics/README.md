# OpenUrzednik.Diagnostics

🇬🇧 [English version](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.en.md)

Adapter, który zamienia spany klientów **OpenUrzednik.NET** na `Activity` z `System.Diagnostics`. Zbierają je OpenTelemetry i inne narzędzia do śledzenia.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Instalacja

```bash
dotnet add package OpenUrzednik.Diagnostics
```

## Użycie

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Gold;

var client = new NbpGoldPriceClient(httpClient,
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

Źródła nazywają się `OpenUrzednik.<Provider>` (np. `OpenUrzednik.Nbp`). W OpenTelemetry włączysz wszystkie naraz:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("OpenUrzednik.*"));
```

`GetShared` tworzy jedno `ActivitySource` na nazwę, które żyje do końca procesu. Własne źródło przekażesz przez `new ActivityTraceSource(source)`.

## Co trafia do śladu

- Każda metoda klienta ma span `<provider>.<obszar>.<operacja>` (np. `nbp.gold.latest`) z parametrami w tagach (np. `nbp.top_count`).
- Zapytanie HTTP to span podrzędny `nbp.http.get` z tagami `http.request.method`, `url.path` i `http.response.status_code`.
- Błąd ustawia status `Error` i zapisuje kod błędu w `error.code`: jako tag spanu albo w zdarzeniu `error`. Wyjątek dodaje zdarzenie `exception`.
- Wszystkie spany są typu `Internal`. Span typu `Client` dla samego zapytania tworzy instrumentacja `HttpClient` i zagnieżdża go pod `nbp.http.get`.

Gdy nikt nie nasłuchuje źródła, adapter nie tworzy `Activity` i niczego nie alokuje.

Na .NET Framework `Activity` domyślnie dostaje identyfikatory hierarchiczne, a nie W3C. OpenTelemetry przełącza aplikację na W3C; bez niego ustaw `Activity.DefaultIdFormat = ActivityIdFormat.W3C` i `Activity.ForceDefaultIdFormat = true`.

## Zależności

Na platformach .NET pakiet nie ma zależności, bo `ActivitySource` jest częścią .NET. Na `netstandard2.0` (.NET Framework) zależy od `System.Diagnostics.DiagnosticSource`.
