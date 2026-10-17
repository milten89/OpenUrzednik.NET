# OpenUrzednik.NET 💼🇵🇱

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Core?style=flat-square)](https://www.nuget.org/)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** to nowoczesny, otwartoźródłowy zestaw bibliotek dla platformy .NET, służący do integracji z polskimi API oraz danymi publicznymi. W aktualnej fazie rozwoju projekt skupia się na wspólnych fundamentach (`OpenUrzednik.Core`) oraz na referencyjnym kliencie API NBP (`OpenUrzednik.Nbp`), na którego wzór powstaną kolejne pakiety.

> ⚠️ **Projekt nieoficjalny:** Ten zestaw bibliotek jest oddolną inicjatywą społecznościową i nie jest powiązany, autoryzowany ani sponsorowany przez żadne z polskich ministerstw ani urzędów państwowych.

---

## 🚀 Dlaczego powstał ten projekt?

Większość istniejących pakietów dla polskich API została porzucona lub nie utrzymuje się w nowoczesnym modelu .NET. OpenUrzednik.NET ma dostarczać:

- **Nowoczesne API:** wersje .NET z długoterminowym wsparciem (LTS), a przez `netstandard2.0` także .NET Framework 4.7.2 lub nowszy ([obsługiwane platformy po angielsku](README.md#supported-platforms))
- **Spójne modele błędów:** `OpenUrzednikResult` / `OpenUrzednikResult<T>` zamiast rozproszonej logiki błędów
- **Elastyczność:** zachowanie klasycznego stylu przez `.EnsureSuccess()` / `.EnsureSuccessAsync()`
- **Przejrzysty rozwój:** kod podzielony na `Core` i osobne pakiety providerów

---

## 📦 Status pakietów

| Pakiet | Status | Opis |
| :--- | :--- | :--- |
| [**OpenUrzednik.Core**](src/OpenUrzednik.Core/README.pl.md) | ✅ Dostępne podstawowe abstrakcje | `OpenUrzednikResult`, `OpenUrzednikResult<T>`, `OpenUrzednikError`, `OpenUrzednikException` oraz metody rozszerzeń `EnsureSuccess` / `EnsureSuccessAsync` |
| [**OpenUrzednik.Http**](src/OpenUrzednik.Http/README.pl.md) | 🧪 Preview | Wspólna warstwa HTTP dla providerów REST/JSON (`RestRequestExecutor`). Używają jej pakiety providerów, nie aplikacje |
| [**OpenUrzednik.Extensions.Logging**](src/OpenUrzednik.Extensions.Logging/README.pl.md) | 🧪 Preview | Przekazuje logi klientów do `ILogger` z Microsoft.Extensions.Logging |
| [**OpenUrzednik.Diagnostics**](src/OpenUrzednik.Diagnostics/README.pl.md) | 🧪 Preview | Zamienia spany klientów na `Activity` dla OpenTelemetry (`AddSource("OpenUrzednik.*")`) |
| [**OpenUrzednik.Nbp**](src/OpenUrzednik.Nbp/README.pl.md) | 🧪 Preview | Kursy walut (tabele A, B, C), tabele kursów i ceny złota z API NBP. API publiczne może się jeszcze zmienić |
| [**OpenUrzednik.Nbp.DependencyInjection**](src/OpenUrzednik.Nbp.DependencyInjection/README.pl.md) | 🧪 Preview | `AddOpenUrzednikNbp()`: klienty NBP w DI z `IHttpClientFactory`, gotowe na `AddStandardResilienceHandler()` |
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.md) | 🚧 Planowany, prace nie ruszyły | Klient GUS (wyszukiwanie firm po REGON/NIP) |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.md) | 🚧 Planowany, prace nie ruszyły | Klient KRS (Krajowy Rejestr Sądowy) |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.md) | 🚧 Planowany, prace nie ruszyły | Klient Białej Listy VAT |

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
catch (OpenUrzednikValidationException ex)
{
    Console.WriteLine(ex.Message);
}
```

### Klienty i DI

Pakiet `OpenUrzednik.Nbp` zawiera klienty `NbpCurrencyExchangeRateClient`, `NbpExchangeRateTableClient` i `NbpGoldPriceClient` (wersja preview). W kontenerze DI zarejestrujesz je pakietem [OpenUrzednik.Nbp.DependencyInjection](src/OpenUrzednik.Nbp.DependencyInjection/README.pl.md). Pakiety `OpenUrzednik.Gus`, `OpenUrzednik.Krs` i `OpenUrzednik.Mf` są dopiero planowane: prace nad nimi ruszą po pierwszym stabilnym wydaniu `OpenUrzednik.Nbp` ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)). Ich wczesne, puste wersje preview na nuget.org są oznaczone jako przestarzałe i ukryte.

### Przykłady

Gotowe programy z klientami NBP, z kontenerem DI i bez niego, znajdziesz w [`samples/`](samples/README.md) (opis po angielsku).

---

## Więcej po angielsku

- [Testy](README.md#tests)

---

## 🤝 Współpraca (Contributing)

Chcesz zgłosić błąd albo zaproponować kolejne źródło danych? Użyj [formularzy issue](https://github.com/milten89/OpenUrzednik.NET/issues/new/choose); nowe źródło danych zgłoś formularzem dla providera. Zobacz też [CONTRIBUTING.md](CONTRIBUTING.md) (po angielsku) — zawiera zasady pracy nad repozytorium, konwencje API oraz instrukcje dla pull requestów.

## 📄 Licencja

Projekt jest dostępny na warunkach licencji MIT. Szczegóły znajdziesz w pliku `LICENSE`.
