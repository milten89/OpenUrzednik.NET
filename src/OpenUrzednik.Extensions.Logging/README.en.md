# OpenUrzednik.Extensions.Logging

🇵🇱 [Wersja polska](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Extensions.Logging/README.md)

Adapter that sends the logs of **OpenUrzednik.NET** clients to `ILogger` from Microsoft.Extensions.Logging.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.en.md) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Extensions.Logging
```

## Usage

```csharp
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp.Gold;

var client = new NbpGoldPriceClient(httpClient,
    logger: loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>());
```

The log category is the client's full type name (e.g. `OpenUrzednik.Nbp.Gold.NbpGoldPriceClient`), as with `ILogger<T>`. You can turn a whole provider's logs on or off by prefix:

```json
{
  "Logging": {
    "LogLevel": {
      "OpenUrzednik.Nbp": "Debug"
    }
  }
}
```

If you already have an `ILogger`, use `new OpenUrzednikLogger(logger)`.

Entries are structured: they carry named values (e.g. `provider`, `path`) and the template in `{OriginalFormat}`, like entries from `LoggerMessage`. Serilog, OpenTelemetry and other providers see them as separate properties. The message text is formatted with the invariant culture.

## Dependencies

The package depends on `Microsoft.Extensions.Logging.Abstractions`. The core and provider packages have their own logging interfaces and don't need this dependency ([ADR-0003](https://github.com/milten89/OpenUrzednik.NET/blob/develop/docs/adr/0003-telemetry-abstractions.md)).
