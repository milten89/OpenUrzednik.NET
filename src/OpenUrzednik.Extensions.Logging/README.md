# OpenUrzednik.Extensions.Logging

Adapter, który przekazuje logi klientów **OpenUrzednik.NET** do `ILogger` z Microsoft.Extensions.Logging.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Instalacja

```bash
dotnet add package OpenUrzednik.Extensions.Logging
```

## Użycie

```csharp
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp.Gold;

var client = new NbpGoldPriceClient(httpClient,
    logger: loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>());
```

Kategorią logów jest pełna nazwa typu klienta (np. `OpenUrzednik.Nbp.Gold.NbpGoldPriceClient`), tak jak w `ILogger<T>`. Logi całego providera włączysz albo wyciszysz prefiksem:

```json
{
  "Logging": {
    "LogLevel": {
      "OpenUrzednik.Nbp": "Debug"
    }
  }
}
```

Jeśli masz już `ILogger`, użyj `new OpenUrzednikLogger(logger)`.

Wpisy są strukturalne: zawierają nazwane wartości (np. `provider`, `path`) i szablon w `{OriginalFormat}`, tak jak wpisy z `LoggerMessage`. Serilog, OpenTelemetry i inni dostawcy widzą je więc jako osobne właściwości. Treść komunikatu jest formatowana z kulturą niezmienną (invariant).

## Zależności

Pakiet zależy od `Microsoft.Extensions.Logging.Abstractions`. Pakiety rdzeniowe i providerów mają własne interfejsy logowania i tej zależności nie potrzebują ([ADR-0003](https://github.com/milten89/OpenUrzednik.NET/blob/develop/docs/adr/0003-telemetry-abstractions.md)).
