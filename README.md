# OpenUrzednik.NET 💼🇵🇱

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Core?style=flat-square)](https://www.nuget.org/)
[![License](https://img.shields.io/github/license/milten89/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** to nowoczesny, otwartoźródłowy zestaw bibliotek dla platformy .NET, służący do integracji z polskimi API oraz danymi publicznymi. W aktualnej fazie rozwoju projekt skupia się przede wszystkim na wspólnych abstrakcjach w `OpenUrzednik.Core` oraz na pierwszych szkieletach pakietów provider-specific.

> ⚠️ **Projekt nieoficjalny:** Ten zestaw bibliotek jest oddolną inicjatywą społecznościową i nie jest powiązany, autoryzowany ani sponsorowany przez żadne z polskich ministerstw ani urzędów państwowych.

---

## 🚀 Dlaczego powstał ten projekt?

Większość istniejących pakietów dla polskich API została porzucona lub nie utrzymuje się w nowoczesnym modelu .NET. OpenUrzednik.NET ma dostarczać:

- **Nowoczesne API:** wsparcie dla `net8.0`, `net9.0` i `net10.0`
- **Spójne modele błędów:** `OpenUrzednikResult` / `OpenUrzednikResult<T>` zamiast rozproszonej logiki błędów
- **Elastyczność:** zachowanie klasycznego stylu przez `.EnsureSuccess()` / `.EnsureSuccessAsync()`
- **Przejrzysty rozwój:** kod podzielony na `Core` i pakiety provider-specific

---

## 📦 Status pakietów

| Pakiet | Status | Opis | Celowy target |
| :--- | :--- | :--- | :--- |
| [**OpenUrzednik.Core**](src/OpenUrzednik.Core/README.md) | ✅ Dostępne podstawowe abstrakcje | `OpenUrzednikResult`, `OpenUrzednikResult<T>`, `OpenUrzednikError`, `OpenUrzednikException` oraz metody rozszerzeń `EnsureSuccess` / `EnsureSuccessAsync` | `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Nbp**](src/OpenUrzednik.Nbp/README.md) | ✅ v1.0.0 | Kursy walut, tabele kursów i ceny złota z API NBP | `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Gus**](src/OpenUrzednik.Gus/README.md) | 🚧 Szkielet | Pakiet przygotowany pod integrację z GUS | `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Krs**](src/OpenUrzednik.Krs/README.md) | 🚧 Szkielet | Pakiet przygotowany pod integrację z KRS | `net8.0`, `net9.0`, `net10.0` |
| [**OpenUrzednik.Mf**](src/OpenUrzednik.Mf/README.md) | 🚧 Szkielet | Pakiet przygotowany pod integrację z Białą Listą VAT | `net8.0`, `net9.0`, `net10.0` |

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

Na tym etapie repozytorium nie ma jeszcze gotowych klientów HTTP ani rozszerzeń DI dla konkretnych providerów. Pakiety `OpenUrzednik.Nbp`, `OpenUrzednik.Gus`, `OpenUrzednik.Krs` i `OpenUrzednik.Mf` są obecnie szkieletem, który będzie rozwijany w kolejnych zmianach.

### Testy

Testy znajdują się w katalogu `tests/` i obejmują przede wszystkim podstawowe zachowania `OpenUrzednik.Core`.

---

## 🤝 Współpraca (Contributing)

Chcesz dodać obsługę kolejnego źródła danych lub zgłosić błąd? Zobacz [CONTRIBUTING.md](CONTRIBUTING.md) — zawiera zasady pracy nad repozytorium, konwencje API oraz instrukcje dla pull requestów.

## 📄 Licencja

Projekt jest dostępny na warunkach licencji MIT. Szczegóły znajdziesz w pliku `LICENSE`.
