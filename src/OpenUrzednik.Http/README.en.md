# OpenUrzednik.Http

🇵🇱 [Wersja polska](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Http/README.md)

Shared HTTP layer for the **OpenUrzednik.NET** packages that use REST/JSON APIs. It sends requests, turns HTTP statuses into `OpenUrzednikResult` errors, tells a timeout apart from a cancellation, and writes logs and spans.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.en.md) for an overview of the project.

## Who it's for

This package is for authors of provider packages (e.g. `OpenUrzednik.Nbp`). You don't add it to your app: it comes with the provider, and the provider's clients hide it behind their API. Its types are in the `OpenUrzednik.Http.Infrastructure` namespace ([ADR-0006](https://github.com/milten89/OpenUrzednik.NET/blob/develop/docs/adr/0006-shared-http-layer.md)).

## Example

```csharp
using OpenUrzednik.Http.Infrastructure;

var profile = new RestProviderProfile("nbp", "NBP API");
var executor = new RestRequestExecutor(httpClient, new Uri("https://api.nbp.pl/api/"), profile,
    timeout: TimeSpan.FromSeconds(10), telemetry: new OpenUrzednikTelemetry(logger, traceSource));

var result = await executor.GetAsync("cenyzlota", MyJsonContext.Default.GoldPriceDtoArray, cancellationToken);
```

`RestRequestExecutor` doesn't change the `HttpClient`, so one `HttpClient` can serve several providers. Errors that happen during normal operation (4xx and 5xx statuses, bad JSON, no connection, a timeout) come back in the result. You get an exception only when the caller cancels the request.

If an API reports errors in an unusual shape, pass a `mapErrorAsync` function in `RestProviderProfile`. It gets an `ErrorResponseContext` with the response and can read up to 500 characters of the body through `ReadMessageAsync()`. When it returns `null`, the default mapping applies.
