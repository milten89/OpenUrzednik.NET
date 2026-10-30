# OpenUrzednik.Extensions.Logging

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Extensions.Logging/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Narodowym Bankiem Polskim (NBP) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Adapter, który przekazuje logi klientów **OpenUrzednik.NET** do `ILogger` z Microsoft.Extensions.Logging.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.Extensions.Logging
```

## Użycie

```csharp
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient,
    logger: loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>());
```

Jeśli masz już `ILogger`, użyj `new OpenUrzednikLogger(logger)`.

## Więcej po angielsku

- [Kategorie logów i ich konfiguracja](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Extensions.Logging/README.md#usage)
- [Zależności](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Extensions.Logging/README.md#dependencies)
