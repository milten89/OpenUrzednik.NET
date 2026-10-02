# OpenUrzednik.Nbp

Klient **NBP** (Narodowy Bank Polski) dla OpenUrzednik.NET — kursy walut oraz ceny złota z API Narodowego Banku Polskiego.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Status

🧪 Wersja preview — dostępne są klienty kursów walut (`NbpCurrencyExchangeRateClient`), tabel kursów (`NbpExchangeRateTableClient`) i cen złota (`NbpGoldPriceClient`). Publiczne API może się jeszcze zmienić przed wydaniem stabilnym.

## Instalacja

```bash
dotnet add package OpenUrzednik.Nbp
```

Wymaga [OpenUrzednik.Core](https://www.nuget.org/packages/OpenUrzednik.Core) i [OpenUrzednik.Http](https://www.nuget.org/packages/OpenUrzednik.Http) (instalowane automatycznie jako zależności).

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

Własny adres (np. proxy albo bramki API) i limit czasu przekazujesz w `NbpOptions`. Klient nie zmienia `HttpClient`, więc możesz go współdzielić z innym kodem:

```csharp
var client = new NbpCurrencyExchangeRateClient(httpClient, new NbpOptions
{
    ApiUrl = "https://proxy.example.com/nbp/",
    Timeout = TimeSpan.FromSeconds(5),
});
```

Z kontenerem DI użyj pakietu [OpenUrzednik.Nbp.DependencyInjection](https://www.nuget.org/packages/OpenUrzednik.Nbp.DependencyInjection): `builder.Services.AddOpenUrzednikNbp()` rejestruje wszystkie trzy klienty, a `.AddStandardResilienceHandler()` dodaje ponowienia.

Klient bierze adres API z pierwszego ustawionego miejsca: `NbpOptions.ApiUrl`, `HttpClient.BaseAddress`, a na końcu domyślny `https://api.nbp.pl/api/`. Jeśli nie ustawisz `NbpOptions.Timeout`, obowiązuje limit czasu `HttpClient` (domyślnie 100 s). `NbpOptions.Timeout` może ten limit skrócić, ale nie wydłużyć, bo `HttpClient.Timeout` nadal obowiązuje do otrzymania nagłówków odpowiedzi. Adres API nie może zawierać zapytania (`?…`) ani fragmentu (`#…`).

Błędy, takie jak brak danych, przekroczony limit zapytań albo upływ limitu czasu, wracają w wyniku, a nie jako wyjątki. Jeśli wolisz wyjątki, wywołaj na wyniku `EnsureSuccess()`.

## Logowanie i śledzenie

Klienty przyjmują opcjonalne parametry `logger` i `traceSource`. Bez nich nic nie logują i nie tworzą spanów. Żeby korzystać z `ILogger` i OpenTelemetry, zainstaluj adaptery [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging) i [OpenUrzednik.Diagnostics](https://www.nuget.org/packages/OpenUrzednik.Diagnostics):

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Gold;

var client = new NbpGoldPriceClient(httpClient,
    logger: loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>(),
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

Spany trafiają do źródła `OpenUrzednik.Nbp`, a logi do kategorii z pełną nazwą klienta (np. `OpenUrzednik.Nbp.Gold.NbpGoldPriceClient`).

Klienty używają własnych interfejsów (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` z OpenUrzednik.Core), a nie `ILogger` i `ActivitySource`. Dzięki temu ten pakiet nie potrzebuje `Microsoft.Extensions.Logging.Abstractions` ani innych pakietów do logowania i śledzenia. Z `ILogger` i `ActivitySource` łączą go dopiero adaptery.
