# OpenUrzednik.NET 💼🇵🇱

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Core?style=flat-square)](https://www.nuget.org/)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** to nowoczesny, otwartoźródłowy zestaw bibliotek dla platformy .NET, służący do integracji z polskimi API oraz danymi publicznymi. W aktualnej fazie rozwoju projekt skupia się na wspólnych fundamentach (`OpenUrzednik.Core`) oraz na referencyjnym kliencie API NBP (`OpenUrzednik.Nbp`), na którego wzór powstaną kolejne pakiety.

> ⚠️ **Projekt nieoficjalny:** Ten zestaw bibliotek jest oddolną inicjatywą społecznościową i nie jest powiązany, autoryzowany ani sponsorowany przez żadne z polskich ministerstw ani urzędów państwowych.

---

## 🚀 Dlaczego powstał ten projekt?

Większość istniejących pakietów dla polskich API została porzucona lub nie utrzymuje się w nowoczesnym modelu .NET. OpenUrzednik.NET ma dostarczać:

- **Nowoczesne API:** aktualne wersje .NET, a przez `netstandard2.0` także .NET Framework ([obsługiwane platformy po angielsku](README.md#-packages))
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
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.md) | 🚧 Szkielet, niepublikowany | Pakiet przygotowany pod integrację z GUS |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.md) | 🚧 Szkielet, niepublikowany | Pakiet przygotowany pod integrację z KRS |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.md) | 🚧 Szkielet, niepublikowany | Pakiet przygotowany pod integrację z Białą Listą VAT |

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

Pakiet `OpenUrzednik.Nbp` zawiera klienty `NbpCurrencyExchangeRateClient`, `NbpExchangeRateTableClient` i `NbpGoldPriceClient` (wersja preview). W kontenerze DI zarejestrujesz je pakietem [OpenUrzednik.Nbp.DependencyInjection](src/OpenUrzednik.Nbp.DependencyInjection/README.pl.md). Pakiety `OpenUrzednik.Gus`, `OpenUrzednik.Krs` i `OpenUrzednik.Mf` są szkieletami i nie ma ich w NuGet — prace nad nimi ruszą po ukończeniu pakietu NBP ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)).

### Przykłady

Gotowe programy z klientami NBP, z kontenerem DI i bez niego, znajdziesz w [`samples/`](samples/README.md) (opis po angielsku).

---

## Więcej po angielsku

- [Testy](README.md#tests)

---

## 🤝 Współpraca (Contributing)

Chcesz dodać obsługę kolejnego źródła danych lub zgłosić błąd? Zobacz [CONTRIBUTING.md](CONTRIBUTING.md) (po angielsku) — zawiera zasady pracy nad repozytorium, konwencje API oraz instrukcje dla pull requestów.

## 📄 Licencja

Projekt jest dostępny na warunkach licencji MIT. Szczegóły znajdziesz w pliku `LICENSE`.
