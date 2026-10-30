# OpenUrzednik.NET 💼🇵🇱

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Nbp?style=flat-square)](https://www.nuget.org/packages/OpenUrzednik.Nbp)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** to nowoczesny, otwartoźródłowy zestaw bibliotek dla platformy .NET, służący do integracji z polskimi API oraz danymi publicznymi. W aktualnej fazie rozwoju projekt skupia się na wspólnych fundamentach (`OpenUrzednik.Core`) oraz na referencyjnym kliencie API NBP (`OpenUrzednik.Nbp`), na którego wzór powstaną kolejne pakiety.

> ⚠️ **Projekt nieoficjalny:** to oddolna inicjatywa społecznościowa. Nie jest powiązana z Narodowym Bankiem Polskim (NBP), ministerstwami, urzędami ani żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

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
| [**OpenUrzednik.Core**](src/OpenUrzednik.Core/README.pl.md) | ✅ Dostępne podstawowe abstrakcje | `OpenUrzednikResult`, `OpenUrzednikResult<T>`, `OpenUrzednikError`, `OpenUrzednikException` oraz `EnsureSuccess()` / `EnsureSuccessAsync()` |
| [**OpenUrzednik.Extensions.Logging**](src/OpenUrzednik.Extensions.Logging/README.pl.md) | 🧪 Preview | Przekazuje logi klientów do `ILogger` z Microsoft.Extensions.Logging |
| [**OpenUrzednik.Diagnostics**](src/OpenUrzednik.Diagnostics/README.pl.md) | 🧪 Preview | Zamienia spany klientów na `Activity` dla OpenTelemetry (`AddSource("OpenUrzednik.*")`) |
| [**OpenUrzednik.Nbp**](src/OpenUrzednik.Nbp/README.pl.md) | 🧪 Preview | Kursy walut (tabele A, B, C), tabele kursów i ceny złota z API NBP. API publiczne może się jeszcze zmienić |
| [**OpenUrzednik.Nbp.DependencyInjection**](src/OpenUrzednik.Nbp.DependencyInjection/README.pl.md) | 🧪 Preview | `AddOpenUrzednikNbp()`: klienty NBP w DI z `IHttpClientFactory`, gotowe na `AddStandardResilienceHandler()` |
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.md) | 🚧 Planowany, prace nie ruszyły | Klient GUS (wyszukiwanie firm po REGON/NIP) |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.md) | 🚧 Planowany, prace nie ruszyły | Klient KRS (Krajowy Rejestr Sądowy) |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.md) | 🚧 Planowany, prace nie ruszyły | Klient Białej Listy VAT |

---

## 🛠️ Szybki start

Zainstaluj klienta NBP:

```bash
dotnet add package OpenUrzednik.Nbp
```

Do wydania 1.0.0 dodaj `--prerelease`: na razie są opublikowane tylko wersje preview.

```csharp
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Gold;

// One HttpClient for the whole app; the clients don't change it.
using var httpClient = new HttpClient();
var gold = new NbpGoldPriceClient(httpClient);
var rates = new NbpCurrencyExchangeRateClient(httpClient);

// Expected failures (no data, timeouts, HTTP errors) come back as errors, not exceptions.
var latest = await gold.GetLatestAsync();
if (latest.IsSuccess)
    Console.WriteLine($"Gold, 1 g: {latest.Value.Price} PLN ({latest.Value.Date:yyyy-MM-dd})");
else
    Console.WriteLine($"Gold price unavailable: {latest.Errors[0].Message}");

// Match handles both cases in one expression.
var usd = await rates.GetTopCountAsync("USD", 5);
Console.WriteLine(usd.Match(
    value => $"USD, last {value.Rates.Count} mid rates: {string.Join(", ", value.Rates.Select(r => r.Price))}",
    errors => $"USD rates unavailable: {errors[0].Message}"));

// Prefer exceptions? EnsureSuccess() throws an OpenUrzednikException subtype for a failed result.
try
{
    GoldPrice price = (await gold.GetAsync(new DateTime(2025, 12, 25))).EnsureSuccess(); // no price on Christmas
    Console.WriteLine(price.Price);
}
catch (OpenUrzednikException ex)
{
    Console.WriteLine($"{ex.GetType().Name} ({ex.Error?.Code}): {ex.Message}");
}
```

Każde wywołanie zwraca `OpenUrzednikResult<T>`. Pracę z wynikiem (`Map`, `Bind`, `TryGetValue`, wyjątki) opisuje [README pakietu Core](src/OpenUrzednik.Core/README.md#working-with-a-result), a pozostałe metody i ograniczenia API opisuje [README pakietu OpenUrzednik.Nbp](src/OpenUrzednik.Nbp/README.md). Oba są po angielsku.

### Klienty i DI

Pakiet `OpenUrzednik.Nbp` zawiera klienty `NbpCurrencyExchangeRateClient`, `NbpExchangeRateTableClient` i `NbpGoldPriceClient` (wersja preview). W kontenerze DI zarejestrujesz je pakietem [OpenUrzednik.Nbp.DependencyInjection](src/OpenUrzednik.Nbp.DependencyInjection/README.pl.md). Pakiety `OpenUrzednik.Gus`, `OpenUrzednik.Krs` i `OpenUrzednik.Mf` są dopiero planowane: prace nad nimi ruszą po pierwszym stabilnym wydaniu `OpenUrzednik.Nbp` ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)). Ich wczesne, puste wersje preview na nuget.org są oznaczone jako przestarzałe i ukryte.

### Przykłady

Gotowe programy z klientami NBP, z kontenerem DI i bez niego, znajdziesz w [`samples/`](samples/README.md) (opis po angielsku).

---

## Więcej po angielsku

- [Testy](README.md#tests)
- [Wersjonowanie i wsparcie](README.md#versioning-and-support)

---

## 🤝 Współpraca (Contributing)

Chcesz zgłosić błąd albo zaproponować kolejne źródło danych? Użyj [formularzy issue](https://github.com/milten89/OpenUrzednik.NET/issues/new/choose); nowe źródło danych zgłoś formularzem dla providera. Zobacz też [CONTRIBUTING.md](CONTRIBUTING.md) (po angielsku) — zawiera zasady pracy nad repozytorium, konwencje API oraz instrukcje dla pull requestów.

## 📄 Licencja

Projekt jest dostępny na warunkach licencji MIT. Szczegóły znajdziesz w pliku `LICENSE`.
