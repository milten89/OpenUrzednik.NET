# OpenUrzednik.Http

Wspólna warstwa HTTP dla pakietów **OpenUrzednik.NET**, które korzystają z API typu REST/JSON. Wysyła zapytania, zamienia statusy HTTP na błędy `OpenUrzednikResult`, odróżnia przekroczenie czasu od anulowania i zapisuje logi oraz spany.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Dla kogo

Pakiet jest dla autorów pakietów providerów (np. `OpenUrzednik.Nbp`). W aplikacji go nie potrzebujesz: instaluje się razem z providerem, a klienty providera ukrywają go za swoim API. Jego typy są w przestrzeni nazw `OpenUrzednik.Http.Infrastructure` ([ADR-0006](https://github.com/milten89/OpenUrzednik.NET/blob/develop/docs/adr/0006-shared-http-layer.md)).

## Przykład

```csharp
using OpenUrzednik.Http.Infrastructure;

var profile = new RestProviderProfile("nbp", "NBP API");
var executor = new RestRequestExecutor(httpClient, new Uri("https://api.nbp.pl/api/"), profile,
    timeout: TimeSpan.FromSeconds(10), telemetry: new OpenUrzednikTelemetry(logger, traceSource));

var result = await executor.GetAsync("cenyzlota", MyJsonContext.Default.GoldPriceDtoArray, cancellationToken);
```

`RestRequestExecutor` nie zmienia `HttpClient`, więc jeden `HttpClient` może obsługiwać kilku providerów. Błędy, które zdarzają się w normalnym działaniu (statusy 4xx i 5xx, błędny JSON, brak połączenia, przekroczony czas), wracają w wyniku. Wyjątek dostajesz tylko wtedy, gdy wywołujący anuluje zapytanie.

Jeśli API zwraca błędy w nietypowej postaci, podaj w `RestProviderProfile` funkcję `mapErrorAsync`. Dostaje ona `ErrorResponseContext` z odpowiedzią i może odczytać do 500 znaków treści przez `ReadMessageAsync()`. Gdy zwróci `null`, obowiązuje domyślne mapowanie.
