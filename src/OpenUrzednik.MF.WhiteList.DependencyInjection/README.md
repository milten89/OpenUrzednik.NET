# OpenUrzednik.MF.WhiteList.DependencyInjection

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by the Polish Ministry of Finance, the National Revenue Administration (KAS) or any other Polish public institution.

Registers the MF White List clients from [OpenUrzednik.MF.WhiteList](https://www.nuget.org/packages/OpenUrzednik.MF.WhiteList) in Microsoft.Extensions.DependencyInjection: typed clients from `IHttpClientFactory`, validated options, logging, tracing and metrics, and `HttpClient` log entries without the identifiers in the request path.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.MF.WhiteList.DependencyInjection
```

## Dependencies

| Package | net10.0 and netstandard2.0 | net8.0 |
|---|---|---|
| `Microsoft.Extensions.Http` | 10.0.x or later | 8.0.x or later |
| `Microsoft.Extensions.Options` | 10.0.x or later | 8.0.x or later |
| `Polly.Core` | 8.x | 8.x |

A .NET 8 app therefore stays on Microsoft.Extensions 8.x. `Polly.Core` is there for the exception types the package recognises when a resilience handler rejects a request. The package also depends on [OpenUrzednik.MF.WhiteList](https://www.nuget.org/packages/OpenUrzednik.MF.WhiteList), [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging) and [OpenUrzednik.Diagnostics](https://www.nuget.org/packages/OpenUrzednik.Diagnostics), which bring their own dependencies.

## Trimming and Native AOT

The .NET builds (`net8.0`, `net10.0`) are trimming- and Native AOT-compatible: publishing an app with trimming or Native AOT gives no warning from this package. CI checks it on every change ([ADR-0014](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0014-trimming-and-native-aot.md)). The `netstandard2.0` build (.NET Framework) isn't covered.

## Usage

```csharp
builder.Services.AddOpenUrzednikMFWhiteList();
```

```csharp
public sealed class PaymentCheck(IWhiteListClient whiteList)
{
    public Task<OpenUrzednikResult<AccountCheckResult>> CheckAsync(string nip, string account, DateTime date, CancellationToken cancellationToken)
        => whiteList.CheckNipBankAccountAsync(nip, account, date, cancellationToken);
}
```

It registers `IWhiteListClient` and `IWhiteListStatusClient` (and their classes). Both use one named `HttpClient` (`"OpenUrzednik.MF.WhiteList"`). The clients are transient, as usual with `IHttpClientFactory`: don't inject them into singletons, because such a singleton keeps one `HttpClient` forever and misses DNS changes. In a `BackgroundService`, inject `IServiceScopeFactory`, create a scope for each run and resolve the client from it, as the [NBP DI README](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Nbp.DependencyInjection/README.md#usage) shows.

Set options in a lambda, for example to use the ministry's test host:

```csharp
builder.Services.AddOpenUrzednikMFWhiteList(options =>
{
    options.ApiUrl = WhiteListOptions.TestApiUrl;
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

`ApiUrl` is the host root, without `api/` or `api2/`. Invalid options (e.g. an `http://` address, or a path ending with `api/`) stop the app at startup with an `OptionsValidationException`.

## Registration rules

- The clients and their `HttpClient` are registered once. Calling `AddOpenUrzednikMFWhiteList` again only adds its `configure` delegate to the options and returns the builder again.
- The clients read the default (unnamed) `WhiteListOptions` from `IOptions<WhiteListOptions>`, which is resolved once per app. They don't pick up a later change, such as a configuration reload.
- Startup validation (`ValidateOnStart`) covers `WhiteListOptions` only. Invalid options throw an `OptionsValidationException` when the host starts, or when the first client is created if there is no host.
- An invalid `HttpClient.BaseAddress` set on the returned builder isn't validated at startup: it throws an `ArgumentException` when a client is created.
- The named `HttpClient` is `WhiteListServiceCollectionExtensions.HttpClientName` (`"OpenUrzednik.MF.WhiteList"`).
- A registered `TimeProvider` replaces `TimeProvider.System`: the clients use it for "today" in the date checks.

## Configuration binding

To read the options from configuration, bind `WhiteListOptions` next to the registration:

```csharp
builder.Services.AddOptions<WhiteListOptions>().BindConfiguration("WhiteList");
builder.Services.AddOpenUrzednikMFWhiteList();
```

```json
{
  "WhiteList": {
    "ApiUrl": "https://wykazpodatnikowvat-test.mf.gov.pl/",
    "Timeout": "00:00:10"
  }
}
```

`Configure<WhiteListOptions>(builder.Configuration.GetSection("WhiteList"))` does the same. Both need the `Microsoft.Extensions.Options.ConfigurationExtensions` package, which ASP.NET Core apps already have. The configuration binder uses reflection; for trimming and Native AOT, enable the [configuration binder source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator).

## Several White List setups

To use the White List in two ways in one app, for example the production host and the test host, or directly and through a gateway, register each setup under a key with `AddKeyedOpenUrzednikMFWhiteList`. Each key gets its own `HttpClient` and options; the default registration can stay alongside:

```csharp
builder.Services.AddOpenUrzednikMFWhiteList();
builder.Services.AddKeyedOpenUrzednikMFWhiteList("test", options => options.ApiUrl = WhiteListOptions.TestApiUrl);
```

Inject a keyed client with `[FromKeyedServices]`:

```csharp
public sealed class PaymentCheck([FromKeyedServices("test")] IWhiteListClient whiteList)
{
    // ...
}
```

The registration rules above apply per key: a second call with the same key only adds its `configure` delegate, the options are named after the key and are read once, and invalid ones stop the app at startup. The key's `HttpClient` is named `WhiteListServiceCollectionExtensions.HttpClientName + ":" + key` (`"OpenUrzednik.MF.WhiteList:test"`). Keyed services need a container that supports them, as Microsoft's dependency injection 8.0 and later does. The keyed clients log under the same categories and trace on the same `OpenUrzednik.MF.WhiteList` source as the default ones; tell the setups apart by the HTTP span's `server.address`.

## Registering the clients yourself

`AddOpenUrzednikMFWhiteList()` covers the usual setup. If you need a client with its own `HttpClient` configuration, create it in a factory lambda:

```csharp
builder.Services.AddHttpClient<IWhiteListClient, WhiteListClient>((httpClient, services) =>
    new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = "https://proxy.example.com/" },
        services.GetService<TimeProvider>()))
    .AddOpenUrzednikMFWhiteListRejectionHandler()
    .AddStandardResilienceHandler();
```

A bare `AddHttpClient<IWhiteListClient, WhiteListClient>()` isn't supported: resolving the client throws an `InvalidOperationException`. A client registered this way gets no logger unless you pass one, records spans and metrics only on .NET 8 and later unless you pass an `OpenUrzednikTelemetry` (see the [base README](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#logging-and-tracing)), gets Microsoft's `HttpClient` log entries with the identifiers in the path (call `RemoveAllLoggers()` on its builder to drop them, see [Logs without identifiers](#logs-without-identifiers)), and turns a resilience handler's rejections into errors only if you call `AddOpenUrzednikMFWhiteListRejectionHandler()` on its builder, as above; see [Rejections](#rejections). Calling it more than once, or on the builder `AddOpenUrzednikMFWhiteList()` returns, adds nothing.

## Logs without identifiers

The requests carry NIP, REGON and account numbers in the URL path, and `IHttpClientFactory`'s own `HttpClient` log entries (`System.Net.Http.HttpClient.{name}.LogicalHandler` and `.ClientHandler`) would write the whole URI. The package replaces them on the named client with entries that write the display path (`search/nip/{nip}`) instead ([ADR-0017](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0017-http-client-logging-for-identifier-paths.md)). `RemoveAllLoggers().AddDefaultLogger()` on the returned builder restores Microsoft's loggers, and with them the identifiers in the entries.

Traces are another matter: OpenTelemetry's `HttpClient` instrumentation still records the real URI as `url.full`, with the identifier in the path. See [the base README](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#logging-and-tracing).

## Resilience

The package doesn't add retries or timeouts by itself. It returns the `IHttpClientBuilder`, so you add them with Microsoft's standard API from the `Microsoft.Extensions.Http.Resilience` package:

```csharp
builder.Services.AddOpenUrzednikMFWhiteList()
    .AddStandardResilienceHandler();
```

By default the handler retries a request up to 3 times on 5xx, 408 and 429 statuses (honoring `Retry-After`). Each attempt gets 10 s, the whole request 30 s. It also has a circuit breaker and a rate limiter. [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) describes the details and how to change the settings. Add one such handler, not several. If the app already adds one to every client through `ConfigureHttpClientDefaults`, don't add a second one: replace it on this client with `RemoveAllResilienceHandlers()` (experimental in `Microsoft.Extensions.Http.Resilience`, warning `EXTEXP0001`) followed by the handler with the filter below, or the default one keeps retrying 429 and 500 `WL-197`. `WhiteListOptions.Timeout` covers all attempts together, so with the handler leave it unset or give it more time than the handler.

The default fits this API badly: a handler decides from the HTTP status and can't see the White List's codes. It retries a 429, though the IP may be blocked until midnight and a retry spends quota, and a 500 `WL-197`, which a retry can't fix. And it doesn't retry the 400 `WL-195` and `WL-196` ("the database was updated, repeat the query" and "update in progress, try later"), which the ministry sends between 22:00 and 24:00 and which a retry fixes. Plug the base package's `WhiteListRetry.ShouldRetryAsync` into the handler's `Retry.ShouldHandle`, and keep `HttpClientResiliencePredicates.IsTransient` for exceptions and timeouts:

```csharp
services.AddOpenUrzednikMFWhiteList().AddStandardResilienceHandler(options =>
    options.Retry.ShouldHandle = args => args.Outcome.Result is { } response
        ? WhiteListRetry.ShouldRetryAsync(response, args.Context.CancellationToken)
        : ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome)));
```

Compared with the default handler, this retries 400 `WL-195` and `WL-196`, and doesn't retry 429 or 500 `WL-197`. It still retries 408 and other 5xx, as before. When it gives up, or doesn't retry, the response goes back to the client unchanged, so the error is the usual one (`WL-195` is a `WhiteListServiceUnavailableError` with its `WhiteListCode` and message). The filter reads at most 4,096 bytes (plus one, to tell a larger body apart) of a 400 or 500 body and replays them, so no data is lost; it looks for the code in the first 500 characters, the part the client's error mapping reads, so both find the same code. The package neither references `Microsoft.Extensions.Http.Resilience` nor exposes Polly types: the snippet is yours, and `WhiteListRetryResilienceTest` in the repository compiles and runs it against the real handler.

On a 429 the error's `RetryAfter` is the server's `Retry-After`, or an estimate up to the next midnight in Warsaw; see [API limits and quota](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#api-limits-and-quota).

### Rejections

When the handler rejects a request, you get an error in the result, not an exception: a timeout is a `RequestTimeoutError`, and an open circuit breaker or rate limiter is a `ServiceUnavailableError` (without a `statusCode`; its `Exception` is an `HttpRequestException` whose `InnerException` is the rejection). A network failure is a `ConnectionFailedError`. Cancellation by the caller still throws `OperationCanceledException`.

### Resilience on .NET Framework

The package runs on .NET Framework 4.7.2 or later, but `AddStandardResilienceHandler()` isn't supported there: the `Microsoft.Extensions.Http.Resilience` 10.x packages warn at build time that they don't support that platform. Build the policy with [Polly](https://www.pollydocs.org) v8 (`Polly.Core` comes with this package) and run it in your own `DelegatingHandler`, added with `AddHttpMessageHandler`. A retry has to send a new request message, because .NET Framework doesn't allow sending the same one twice; the White List requests are `GET` requests without a body, so copying the method, address, headers and properties is enough. The properties matter: they carry the display path that the logs write instead of the identifier, and a request without them is logged with the real path (see [Logs without identifiers](#logs-without-identifiers)). The same `WhiteListRetry` decides about responses:

```csharp
using Polly;
using Polly.Retry;
using Polly.Timeout;

sealed class WhiteListResilienceHandler : DelegatingHandler
{
    private static readonly ResiliencePipeline<HttpResponseMessage> Pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = args => args.Outcome.Result is { } response
                ? WhiteListRetry.ShouldRetryAsync(response, args.Context.CancellationToken)
                : new ValueTask<bool>(args.Outcome.Exception is HttpRequestException or TimeoutRejectedException),
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
                foreach (var property in request.Properties)
                    attempt.Properties[property.Key] = property.Value;
                return await base.SendAsync(attempt, token).ConfigureAwait(false);
            },
            cancellationToken).AsTask();
}
```

```csharp
builder.Services.AddOpenUrzednikMFWhiteList()
    .AddHttpMessageHandler(() => new WhiteListResilienceHandler());
```

This handler doesn't honor `Retry-After`. The package treats what such a handler throws like what Microsoft's handler throws, because its own handler sits outside every handler added to the `HttpClient`: a `TimeoutRejectedException` becomes a `RequestTimeoutError`, and a `BrokenCircuitException` or `RateLimiterRejectedException` (any Polly `ExecutionRejectedException`) becomes a `ServiceUnavailableError` without a `statusCode`. A network failure that exhausts the retries is a `ConnectionFailedError`.

## Logging and tracing

The clients log through the registered `ILoggerFactory`. The category is the client's full type name (e.g. `OpenUrzednik.MF.WhiteList.WhiteListClient`). Spans go to the `OpenUrzednik.MF.WhiteList` source, which you enable in OpenTelemetry with `AddSource("OpenUrzednik.*")`. Operation durations go to an `OpenUrzednik.MF.WhiteList` meter on every target, .NET Framework included: the container's `IMeterFactory` meter when it has one (on .NET 8 and later `AddHttpClient()` registers it), else the shared one ([details](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#with-dependency-injection)); enable it with `AddMeter("OpenUrzednik.*")`. Every log entry has an event id from `WhiteListLogEvents`; the span names, tags, [metrics](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#metrics) and event ids are listed in the [telemetry reference](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.md#telemetry-reference).
