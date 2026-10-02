# OpenUrzednik.Nbp.DependencyInjection

Rejestracja klientów **NBP** z [OpenUrzednik.Nbp](https://www.nuget.org/packages/OpenUrzednik.Nbp) w Microsoft.Extensions.DependencyInjection: klienty typowane z `IHttpClientFactory`, sprawdzane opcje, logowanie i śledzenie.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Instalacja

```bash
dotnet add package OpenUrzednik.Nbp.DependencyInjection
```

## Użycie

```csharp
builder.Services.AddOpenUrzednikNbp();
```

```csharp
public sealed class RatesService(INbpCurrencyExchangeRateClient rates)
{
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetUsdAsync(CancellationToken cancellationToken)
        => rates.GetLatestAsync("USD", cancellationToken: cancellationToken);
}
```

Rejestrowane są `INbpCurrencyExchangeRateClient`, `INbpExchangeRateTableClient` i `INbpGoldPriceClient` (oraz ich klasy). Wszystkie korzystają z jednego nazwanego `HttpClient` (`"OpenUrzednik.Nbp"`). Klienty są rejestrowane jako transient, jak zwykle przy `IHttpClientFactory`: nie wstrzykuj ich do singletonów, bo taki singleton zatrzyma jeden `HttpClient` na zawsze i nie zauważy zmian DNS.

Opcje ustawiasz w lambdzie:

```csharp
builder.Services.AddOpenUrzednikNbp(options =>
{
    options.ApiUrl = "https://proxy.example.com/nbp/";
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

Błędne opcje (np. adres `http://`) zatrzymują start aplikacji wyjątkiem `OptionsValidationException`.

## Ponowienia i limity czasu

Pakiet sam nie dodaje ponowień ani limitów czasu. Zwraca `IHttpClientBuilder`, więc dodajesz je standardowym API Microsoftu z pakietu `Microsoft.Extensions.Http.Resilience`:

```csharp
builder.Services.AddOpenUrzednikNbp()
    .AddStandardResilienceHandler();
```

Domyślnie handler ponawia zapytanie do 3 razy przy statusach 5xx, 408 i 429 (z uwzględnieniem `Retry-After`). Każda próba ma 10 s, całe zapytanie 30 s. Handler ma też circuit breaker i rate limiter. Szczegóły i zmianę ustawień (np. `options.Retry.MaxRetryAttempts = 5`) opisuje [dokumentacja Microsoftu](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience). Dodaj jeden taki handler, nie kilka. Jeśli aplikacja dodaje go już wszystkim klientom przez `ConfigureHttpClientDefaults` (jak szablon .NET Aspire), nie dodawaj drugiego.

Gdy handler odrzuci zapytanie, dostajesz błąd w wyniku, a nie wyjątek: przekroczony czas to `RequestTimeoutError`, a otwarty circuit breaker albo rate limiter to `ServiceUnavailableError`. Anulowanie przez wywołującego nadal rzuca `OperationCanceledException`.

## Logowanie i śledzenie

Klienty logują przez zarejestrowany `ILoggerFactory`. Kategorią jest pełna nazwa klienta (np. `OpenUrzednik.Nbp.Gold.NbpGoldPriceClient`). Spany trafiają do źródła `OpenUrzednik.Nbp`, które w OpenTelemetry włączysz przez `AddSource("OpenUrzednik.*")`.

Jeśli zarejestrujesz własny `TimeProvider` albo `INbpUrlBuilderFactory`, klienty użyją ich zamiast domyślnych.
