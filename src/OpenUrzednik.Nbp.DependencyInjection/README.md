# OpenUrzednik.Nbp.DependencyInjection

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp.DependencyInjection/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by Narodowy Bank Polski (NBP) or any other Polish public institution.

Registers the **NBP** clients from [OpenUrzednik.Nbp](https://www.nuget.org/packages/OpenUrzednik.Nbp) in Microsoft.Extensions.DependencyInjection: typed clients from `IHttpClientFactory`, validated options, logging and tracing.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Nbp.DependencyInjection
```

## Dependencies

| Package | net10.0 and netstandard2.0 | net8.0 |
|---|---|---|
| `Microsoft.Extensions.Http` | 10.0.x or later | 8.0.x or later |
| `Microsoft.Extensions.Options` | 10.0.x or later | 8.0.x or later |
| `Polly.Core` | 8.x | 8.x |

A .NET 8 app therefore stays on Microsoft.Extensions 8.x. `Polly.Core` is there for the exception types the package recognises when a resilience handler rejects a request. The package also depends on [OpenUrzednik.Nbp](https://www.nuget.org/packages/OpenUrzednik.Nbp), [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging) and [OpenUrzednik.Diagnostics](https://www.nuget.org/packages/OpenUrzednik.Diagnostics), which bring their own dependencies.

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

A `BackgroundService` is a singleton, so it can't take a client in its constructor. Inject `IServiceScopeFactory`, create a scope for each run (each loop iteration or job) and resolve the client from it:

```csharp
public sealed class RatesWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            using var scope = scopeFactory.CreateScope();
            var rates = scope.ServiceProvider.GetRequiredService<INbpCurrencyExchangeRateClient>();

            var result = await rates.GetLatestAsync("USD", cancellationToken: stoppingToken);
            // Use result.
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
```

Set options in a lambda:

```csharp
builder.Services.AddOpenUrzednikNbp(options =>
{
    options.ApiUrl = "https://proxy.example.com/nbp/";
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

Invalid options (e.g. an `http://` address) stop the app at startup with an `OptionsValidationException`.

## Registration rules

- The clients and their `HttpClient` are registered once. Calling `AddOpenUrzednikNbp` again only adds its `configure` delegate to the options and returns the builder again.
- The clients read the default (unnamed) `NbpOptions` from `IOptions<NbpOptions>`, which is resolved once per app. They don't pick up a later change, such as a configuration reload.
- Startup validation (`ValidateOnStart`) covers `NbpOptions` only. Invalid options throw an `OptionsValidationException` when the host starts, or when the first client is created if there is no host.
- An invalid `HttpClient.BaseAddress` set on the returned builder isn't validated at startup: it throws an `ArgumentException` when a client is created.
- The named `HttpClient` is `NbpServiceCollectionExtensions.HttpClientName` (`"OpenUrzednik.Nbp"`).

## Configuration binding

To read the options from configuration, bind `NbpOptions` next to the registration:

```csharp
builder.Services.AddOptions<NbpOptions>().BindConfiguration("Nbp");
builder.Services.AddOpenUrzednikNbp();
```

```json
{
  "Nbp": {
    "ApiUrl": "https://proxy.example.com/nbp/",
    "Timeout": "00:00:10"
  }
}
```

`Configure<NbpOptions>(builder.Configuration.GetSection("Nbp"))` does the same. Both need the `Microsoft.Extensions.Options.ConfigurationExtensions` package, which ASP.NET Core apps already have. The configuration binder uses reflection; for trimming and Native AOT, enable the [configuration binder source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator).

## Registering the clients yourself

`AddOpenUrzednikNbp()` covers the usual setup. If you need a client with its own `HttpClient` configuration, create it in a factory lambda:

```csharp
builder.Services.AddHttpClient<INbpGoldPriceClient, NbpGoldPriceClient>((httpClient, services) =>
    new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = "https://proxy.example.com/nbp/" },
        services.GetService<TimeProvider>()));
```

A bare `AddHttpClient<INbpGoldPriceClient, NbpGoldPriceClient>()` isn't supported: resolving the client throws an `InvalidOperationException` ("A suitable constructor … could not be located"). A client registered this way gets no logger unless you pass one (`loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>()`), records spans only on .NET 8 and later unless you pass `ActivityTraceSource.GetShared(NbpTelemetry.SourceName)`, and a resilience handler's rejections aren't turned into errors (see below).

## Retries and timeouts

The package doesn't add retries or timeouts by itself. It returns the `IHttpClientBuilder`, so you add them with Microsoft's standard API from the `Microsoft.Extensions.Http.Resilience` package:

```csharp
builder.Services.AddOpenUrzednikNbp()
    .AddStandardResilienceHandler();
```

`NbpOptions.Timeout` covers all attempts together, so if it's shorter than the handler's total timeout, it cuts the retries short. With the handler, leave it unset or give it more time than the handler.

By default the handler retries a request up to 3 times on 5xx, 408 and 429 statuses (honoring `Retry-After`). Each attempt gets 10 s, the whole request 30 s. The handler also has a circuit breaker and a rate limiter. [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) describes the details and how to change the settings (e.g. `options.Retry.MaxRetryAttempts = 5`). Add one such handler, not several. If the app already adds one to every client through `ConfigureHttpClientDefaults` (like the .NET Aspire template), don't add a second one. The package runs on .NET Framework 4.7.2 or later, but `AddStandardResilienceHandler()` isn't supported there: the `Microsoft.Extensions.Http.Resilience` 10.x packages warn at build time that they don't support that platform.

When the handler rejects a request, you get an error in the result, not an exception: a timeout is a `RequestTimeoutError`, and an open circuit breaker or rate limiter is a `ServiceUnavailableError` (without a `statusCode`; its `Exception` is an `HttpRequestException` whose `InnerException` is the rejection). A network failure is a `ConnectionFailedError`. Cancellation by the caller still throws `OperationCanceledException`.

### Resilience on .NET Framework

On .NET Framework, build the policy with [Polly](https://www.pollydocs.org) v8 (`Polly.Core` comes with this package) and run it in your own `DelegatingHandler`, added with `AddHttpMessageHandler`. A retry has to send a new request message, because .NET Framework doesn't allow sending the same one twice; the NBP requests are `GET` requests without a body, so copying the method, address and headers is enough.

```csharp
using Polly;
using Polly.Retry;
using Polly.Timeout;

sealed class NbpResilienceHandler : DelegatingHandler
{
    private static readonly ResiliencePipeline<HttpResponseMessage> Pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .HandleResult(response => (int)response.StatusCode >= 500
                    || response.StatusCode == HttpStatusCode.RequestTimeout
                    || (int)response.StatusCode == 429),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                args.Outcome.Result?.Dispose();
                return default;
            },
        })
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Pipeline.ExecuteAsync(
            async token =>
            {
                using var attempt = new HttpRequestMessage(request.Method, request.RequestUri);
                foreach (var header in request.Headers)
                    attempt.Headers.TryAddWithoutValidation(header.Key, header.Value);
                return await base.SendAsync(attempt, token).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
}
```

```csharp
builder.Services.AddOpenUrzednikNbp()
    .AddHttpMessageHandler(() => new NbpResilienceHandler());
```

This handler doesn't honor `Retry-After`. The package treats what such a handler throws like what Microsoft's handler throws, because its own handler sits outside every handler added to the `HttpClient`: a `TimeoutRejectedException` becomes a `RequestTimeoutError`, and a `BrokenCircuitException` or `RateLimiterRejectedException` (any Polly `ExecutionRejectedException`) becomes a `ServiceUnavailableError` without a `statusCode`. A network failure that exhausts the retries is a `ConnectionFailedError`.

## Logging and tracing

The clients log through the registered `ILoggerFactory`. The category is the client's full type name (e.g. `OpenUrzednik.Nbp.NbpGoldPriceClient`). Spans go to the `OpenUrzednik.Nbp` source, which you enable in OpenTelemetry with `AddSource("OpenUrzednik.*")`. Every log entry has an event id from `NbpLogEvents`; the span names, tags and event ids are listed in the [telemetry reference](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md#telemetry-reference).

If you register your own `TimeProvider`, the clients use it instead of `TimeProvider.System`.
