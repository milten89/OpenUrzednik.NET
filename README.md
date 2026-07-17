# OpenUrzednik.NET 💼🇵🇱

[![Nuget](https://img.shields.io/nuget/v/OpenUrzednik.Core?style=flat-square)](https://www.nuget.org/)
[![License](https://img.shields.io/github/license/milten98/OpenUrzednik.NET?style=flat-square)](LICENSE)

**OpenUrzednik.NET** to nowoczesny, wydajny i w pełni otwartoźródłowy (Open Source) zestaw bibliotek dla platformy .NET, służący do integracji z polskimi bazami danych oraz API instytucji publicznych.

> ⚠️ **Projekt nieoficjalny:** Ten zestaw bibliotek jest oddolną inicjatywą społecznościową i nie jest w żaden sposób powiązany, autoryzowany ani sponsorowany przez żadne z polskich ministerstw ani urzędów państwowych.

---

## 🚀 Dlaczego powstał ten projekt?

Większość istniejących paczek NuGet dla polskich API (GUS, Biała Lista, NBP) została porzucona lata temu. OpenUrzednik.NET powstał, aby dostarczyć rozwiązanie:

* **Zorientowane na wydajność:** Pełne wsparcie dla asynchroniczności, `IHttpClientFactory` oraz optymalnego parsowania JSON.
* **Nowoczesne:** Wykorzystuje architekturę .NET 10/9/8 oraz mechanizmy C# 14/13.
* **Wstecznie kompatybilne:** Dzięki kompilacji do `.NET Standard 2.0` biblioteki działają również w starszych systemach opartych na `.NET Framework 4.7.2+`.
* **Elastyczne:** Sam wybierasz styl obsługi błędów – monadyczny (`Result<T>`) lub tradycyjny (wyjątki).

---

## 📦 Status Pakietów

| Paczka NuGet | Status API | Opis | Podstawowy Target |
| :--- | :--- | :--- | :--- |
| **OpenUrzednik.Core** | 📅 Planowane | Wspólny rdzeń, mechanizmy retry (Polly), modele błędów | `netstandard2.0`, `net8.0+` |
| **OpenUrzednik.Nbp** | 📅 Planowane | Kursy walut i tabele NBP | `netstandard2.0`, `net8.0+` |
| **OpenUrzednik.Gus** | 📅 Planowane | Rejestr REGON (GUS BIR 1.1) | `netstandard2.0`, `net8.0+` |
| **OpenUrzednik.Krs** | 📅 Planowane | Krajowy Rejestr Sądowy (KRS) | `netstandard2.0`, `net8.0+` |
| **OpenUrzednik.Mf** | 📅 Planowane | Ministerstwo finansów (Biała lista VAT) | `netstandard2.0`, `net8.0+` |

---

## 🛠️ Szybki Start (Przykład: NBP)

Biblioteka nie narzuca jednego stylu programowania. Oferuje pełną swobodę wyboru.

### Opcja A: Podejście wydajnościowe (Monadyczne `Result<T>`)

Idealne do systemów o wysokiej wydajności. Przewidywalne sytuacje biznesowe (np. brak waluty) nie alokują zasobów na rzucanie wyjątków. Wyjątek poleci tylko wtedy, gdy np. padnie sieć.

```csharp
using OpenUrzednik.Nbp;

var client = new NbpClient(httpClient);

// Zwraca strukturę Result zamiast rzucać błędem 404
var result = await client.GetExchangeRateResultAsync("USD");

if (result.IsSuccess)
{
    Console.WriteLine($"Kurs USD: {result.Value.Rate}");
}
else
{
    Console.WriteLine($"Błąd biznesowy: {result.Error}");
}
```

### Opcja B: Podejście tradycyjne (Wyjątki)

Jeśli wolisz klasyczny styl .NET oparty na blokach `try-catch` lub globalnych filtrach wyjątków:

```csharp
using OpenUrzednik.Nbp;

var client = new NbpClient(httpClient);

try
{
    // Rzuci OpenUrzednikDomainException w przypadku błędu 404/500
    var data = await client.GetExchangeRateAsync("XYZ"); 
    Console.WriteLine($"Kurs: {data.Rate}");
}
catch (OpenUrzednikDomainException ex)
{
    Console.WriteLine($"Błąd domeny: {ex.Message}");
}
```

## 🤝 Współpraca (Contributing)

Chcesz dodać obsługę kolejnego urzędu lub zgłosić błąd? Pociągnij repozytorium, utwórz nową gałąź i podeślij Pull Request! Projekt korzysta z Central Package Management (CPM), co ułatwia zarządzanie zależnościami.

1. Sklonuj repozytorium.
2. Uruchom `dotnet restore`.
3. Dodaj swój kod i upewnij się, że testy w folderze `/tests` przechodzą pomyślnie (`dotnet test`).

## 📄 Licencja

Projekt jest dostępny na warunkach licencji MIT. Szczegóły znajdziesz w pliku `LICENSE`.
