# OpenUrzednik.Nbp.DependencyInjection

🇵🇱 [Wersja polska](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp.DependencyInjection/README.md)

Registers the **NBP** clients from [OpenUrzednik.Nbp](https://www.nuget.org/packages/OpenUrzednik.Nbp) in Microsoft.Extensions.DependencyInjection: typed clients from `IHttpClientFactory`, validated options, logging and tracing.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.en.md) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Nbp.DependencyInjection
```

## Usage

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

It registers `INbpCurrencyExchangeRateClient`, `INbpExchangeRateTableClient` and `INbpGoldPriceClient` (and their classes). All of them use one named `HttpClient` (`"OpenUrzednik.Nbp"`). The clients are transient, as usual with `IHttpClientFactory`: don't inject them into singletons, because such a singleton keeps one `HttpClient` forever and misses DNS changes.

Set options in a lambda:

```csharp
builder.Services.AddOpenUrzednikNbp(options =>
{
    options.ApiUrl = "https://proxy.example.com/nbp/";
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

Invalid options (e.g. an `http://` address) stop the app at startup with an `OptionsValidationException`.

## Retries and timeouts

The package doesn't add retries or timeouts by itself. It returns the `IHttpClientBuilder`, so you add them with Microsoft's standard API from the `Microsoft.Extensions.Http.Resilience` package:

```csharp
builder.Services.AddOpenUrzednikNbp()
    .AddStandardResilienceHandler();
```

By default the handler retries a request up to 3 times on 5xx, 408 and 429 statuses (honoring `Retry-After`). Each attempt gets 10 s, the whole request 30 s. The handler also has a circuit breaker and a rate limiter. [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) describes the details and how to change the settings (e.g. `options.Retry.MaxRetryAttempts = 5`). Add one such handler, not several. If the app already adds one to every client through `ConfigureHttpClientDefaults` (like the .NET Aspire template), don't add a second one. On .NET Framework, the `Microsoft.Extensions.Http.Resilience` 10.x packages warn at build time that they don't support that platform.

When the handler rejects a request, you get an error in the result, not an exception: a timeout is a `RequestTimeoutError`, and an open circuit breaker or rate limiter is a `ServiceUnavailableError`. Cancellation by the caller still throws `OperationCanceledException`.

## Logging and tracing

The clients log through the registered `ILoggerFactory`. The category is the client's full type name (e.g. `OpenUrzednik.Nbp.Gold.NbpGoldPriceClient`). Spans go to the `OpenUrzednik.Nbp` source, which you enable in OpenTelemetry with `AddSource("OpenUrzednik.*")`.

If you register your own `TimeProvider` or `INbpUrlBuilderFactory`, the clients use them instead of the defaults.
