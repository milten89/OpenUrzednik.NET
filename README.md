# OpenUrzednik.NET 💼🇵🇱

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Core?style=flat-square)](https://www.nuget.org/)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** to nowoczesny, otwartoźródłowy zestaw bibliotek dla platformy .NET, służący do integracji z polskimi API oraz danymi publicznymi. W aktualnej fazie rozwoju projekt skupia się na wspólnych fundamentach (`OpenUrzednik.Core`) oraz na referencyjnym kliencie API NBP (`OpenUrzednik.Nbp`), na którego wzór powstaną kolejne pakiety.

> ⚠️ **Projekt nieoficjalny:** Ten zestaw bibliotek jest oddolną inicjatywą społecznościową i nie jest powiązany, autoryzowany ani sponsorowany przez żadne z polskich ministerstw ani urzędów państwowych.

---

## 🚀 Dlaczego powstał ten projekt?

Większość istniejących pakietów dla polskich API została porzucona lub nie utrzymuje się w nowoczesnym modelu .NET. OpenUrzednik.NET ma dostarczać:

- **Nowoczesne API:** wsparcie dla `net8.0`, `net9.0` i `net10.0`, a przez `netstandard2.0` także dla .NET Framework (testujemy na .NET Framework 4.7.2)
- **Spójne modele błędów:** `OpenUrzednikResult` / `OpenUrzednikResult<T>` zamiast rozproszonej logiki błędów
- **Elastyczność:** zachowanie klasycznego stylu przez `.EnsureSuccess()` / `.EnsureSuccessAsync()`
- **Przejrzysty rozwój:** kod podzielony na `Core` i pakiety provider-specific

---

## 📦 Status pakietów

| Pakiet | Status | Opis | Celowy target |
| :--- | :--- | :--- | :--- |
| [**OpenUrzednik.Core**](src/OpenUrzednik.Core/README.md) | ✅ Dostępne podstawowe abstrakcje | `OpenUrzednikResult`, `OpenUrzednikResult<T>`, `OpenUrzednikError`, `OpenUrzednikException` oraz metody rozszerzeń `EnsureSuccess` / `EnsureSuccessAsync` | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Http**](src/OpenUrzednik.Http/README.md) | 🧪 Preview | Wspólna warstwa HTTP dla providerów REST/JSON (`RestRequestExecutor`). Używają jej pakiety providerów, nie aplikacje | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Extensions.Logging**](src/OpenUrzednik.Extensions.Logging/README.md) | 🧪 Preview | Przekazuje logi klientów do `ILogger` z Microsoft.Extensions.Logging | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Diagnostics**](src/OpenUrzednik.Diagnostics/README.md) | 🧪 Preview | Zamienia spany klientów na `Activity` dla OpenTelemetry (`AddSource("OpenUrzednik.*")`) | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Nbp**](src/OpenUrzednik.Nbp/README.md) | 🧪 Preview | Kursy walut (tabele A, B, C), tabele kursów i ceny złota z API NBP. API publiczne może się jeszcze zmienić | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Nbp.DependencyInjection**](src/OpenUrzednik.Nbp.DependencyInjection/README.md) | 🧪 Preview | `AddOpenUrzednikNbp()`: klienty NBP w DI z `IHttpClientFactory`, gotowe na `AddStandardResilienceHandler()` | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.md) | 🚧 Szkielet | Pakiet przygotowany pod integrację z GUS | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.md) | 🚧 Szkielet | Pakiet przygotowany pod integrację z KRS | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.md) | 🚧 Szkielet | Pakiet przygotowany pod integrację z Białą Listą VAT | `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |

---

## 🛠️ Szybki start

Aktualnie najłatwiej zacząć od `OpenUrzednik.Core`.

### Przykład użycia `OpenUrzednikResult`

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;

OpenUrzednikResult<int> result = OpenUrzednikResult.Success(42);
int value = result.EnsureSuccess();
Console.WriteLine(value);
```

Przykład błędu biznesowego:

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

### Uwaga o DI i klientach

Pakiet `OpenUrzednik.Nbp` zawiera klienty `NbpCurrencyExchangeRateClient`, `NbpExchangeRateTableClient` i `NbpGoldPriceClient` (wersja preview — sposób tworzenia klientów jeszcze się zmieni, zob. [ADR-0007](docs/adr/0007-client-api-and-extensibility.md)). Rozszerzenia DI pojawią się w osobnych pakietach `*.DependencyInjection`. Pakiety `OpenUrzednik.Gus`, `OpenUrzednik.Krs` i `OpenUrzednik.Mf` są szkieletami — prace nad nimi ruszą po ukończeniu pakietu NBP ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)).

### Testy

Testy znajdują się w katalogu `tests/`: testy jednostkowe, testy HTTP z WireMockiem oraz (opcjonalne, uruchamiane ręcznie) testy na prawdziwym API.

---

## 🤝 Współpraca (Contributing)

Chcesz dodać obsługę kolejnego źródła danych lub zgłosić błąd? Zobacz [CONTRIBUTING.md](CONTRIBUTING.md) — zawiera zasady pracy nad repozytorium, konwencje API oraz instrukcje dla pull requestów.

## 📄 Licencja

Projekt jest dostępny na warunkach licencji MIT. Szczegóły znajdziesz w pliku `LICENSE`.
