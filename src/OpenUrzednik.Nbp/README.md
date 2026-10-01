# OpenUrzednik.Nbp

Klient **NBP** (Narodowy Bank Polski) dla OpenUrzednik.NET — kursy walut oraz ceny złota z API Narodowego Banku Polskiego.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Status

🧪 Wersja preview — dostępne są klienty kursów walut (`NbpCurrencyExchangeRateClient`), tabel kursów (`NbpExchangeRateTableClient`) i cen złota (`NbpGoldPriceClient`). Publiczne API może się jeszcze zmienić przed wydaniem stabilnym.

## Instalacja

```bash
dotnet add package OpenUrzednik.Nbp
```

Wymaga [OpenUrzednik.Core](https://www.nuget.org/packages/OpenUrzednik.Core) (instalowane automatycznie jako zależność).
