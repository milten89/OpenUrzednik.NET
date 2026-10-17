# OpenUrzednik.Nbp

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

Klient **NBP** (Narodowy Bank Polski) dla OpenUrzednik.NET — kursy walut oraz ceny złota z API Narodowego Banku Polskiego.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.Nbp
```

## Użycie

Najprostszy wariant: domyślny adres API NBP i limit czasu z `HttpClient`.

```csharp
using var httpClient = new HttpClient();
var client = new NbpGoldPriceClient(httpClient);

var result = await client.GetLatestAsync();
if (result.IsSuccess)
    Console.WriteLine($"{result.Value.Date}: {result.Value.Price} zł");
else
    Console.WriteLine(result.Errors[0].Message);
```

Z kontenerem DI użyj pakietu [OpenUrzednik.Nbp.DependencyInjection](https://www.nuget.org/packages/OpenUrzednik.Nbp.DependencyInjection): `builder.Services.AddOpenUrzednikNbp()` rejestruje wszystkie trzy klienty, a `.AddStandardResilienceHandler()` dodaje ponowienia.

Błędy, takie jak brak danych, przekroczony limit zapytań albo upływ limitu czasu, wracają w wyniku, a nie jako wyjątki. Jeśli wolisz wyjątki, wywołaj na wyniku `EnsureSuccess()`.

## Więcej po angielsku

- [Własny adres API, limit czasu i błędy](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md#usage)
- [.NET Framework i daty](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md#net-framework)
- [Logowanie i śledzenie](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md#logging-and-tracing)
