# Przykłady

🇬🇧 [English version](README.en.md)

Dwa programy, które wywołują prawdziwe API NBP. Oba korzystają z projektów z `src/`, więc pokazują aktualny stan kodu, a nie wersję z NuGet.

| Projekt | Co pokazuje |
| :--- | :--- |
| [`OpenUrzednik.Samples.Console`](OpenUrzednik.Samples.Console/Program.cs) | Klienty NBP bez kontenera DI: sprawdzanie wyniku, `Match`, `TryGetValue`, błąd walidacji i `EnsureSuccess()` z wyjątkiem |
| [`OpenUrzednik.Samples.WebApi`](OpenUrzednik.Samples.WebApi/Program.cs) | Minimal API z `AddOpenUrzednikNbp()` i `AddStandardResilienceHandler()`; błędy z wyniku zamienia na statusy HTTP i problem details |

## Uruchomienie

```bash
dotnet run --project samples/OpenUrzednik.Samples.Console -f net10.0
dotnet run --project samples/OpenUrzednik.Samples.WebApi -f net10.0
```

Web API słucha na `http://localhost:5000` i odpowiada m.in. na `/gold/latest`, `/rates/USD?topCount=5` i `/tables/c/latest`. Zapytanie `/rates/USD?topCount=0` zwraca 400 z błędem walidacji, a brak danych 404.
