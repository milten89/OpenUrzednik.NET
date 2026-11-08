# OpenUrzednik.Transport.Http

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Transport.Http/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by Narodowy Bank Polski (NBP) or any other Polish public institution.

The HTTP transport types of **OpenUrzednik.NET**: the errors a REST client returns when an HTTP response caused them, and their status code.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

Every REST provider (e.g. `OpenUrzednik.Nbp`) depends on this package, so you get it with the provider. Install it on its own only if you build your own client in the same style:

```bash
dotnet add package OpenUrzednik.Transport.Http
```

## Usage

`OpenUrzednik.Core` describes conditions (not found, rate limited, unavailable), whatever the transport. When an HTTP response caused the error, a client returns the condition's HTTP subclass from this package:

| HTTP response | Error | It is a |
|---|---|---|
| 400 | `HttpBadRequestError` | `BadRequestError` |
| 401, 403 | `HttpUnauthorizedError` | `UnauthorizedError` |
| 404 | `HttpNotFoundError` | `NotFoundError` |
| 408 | `HttpRequestTimeoutError` | `RequestTimeoutError` |
| 429 | `HttpRateLimitExceededError` | `RateLimitExceededError` |
| 5xx | `HttpServiceUnavailableError` | `ServiceUnavailableError` |
| another status | `HttpUnknownError` | `UnknownError` |

A provider may map a status to another condition (e.g. a quota answered with 400); the error is then that condition's HTTP subclass. Every subclass keeps its condition's `Code` and exception, so your `is NotFoundError` and `Code` checks handle an `HttpNotFoundError` like any other `NotFoundError`. Read the status through `IOpenUrzednikHttpStatusCode`:

```csharp
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Transport.Http;

var result = await client.GetAsync(date);
if (result.IsFailure && result.Errors[0] is IOpenUrzednikHttpStatusCode http)
    Console.WriteLine($"HTTP {http.StatusCode}");

// Or, without a type check: null when no HTTP response caused the error.
int? status = result.Errors[0].GetHttpStatusCode();
```

An error with no HTTP response behind it is Core's plain type: an empty answer (`NotFoundError`), a client-side timeout (`RequestTimeoutError`), a resilience handler's rejection (`ServiceUnavailableError`) or a network failure (`ConnectionFailedError`).

`GetHttpStatusCode()` and `TryGetHttpStatusCode()` are in Core (`OpenUrzednik.Core.Errors.HttpErrorMetadata`) in 1.x; they are planned to move to this package in 2.0. Until then the HTTP errors also store their status in `Metadata` under `"statusCode"`.

## Compatibility

`IOpenUrzednikHttpStatusCode` never gets a new member: a further HTTP detail comes as a separate interface. Code that compares the exact type (`error.GetType() == typeof(NotFoundError)`) doesn't match an HTTP error; use `is` or `Code`.

## Dependencies

`OpenUrzednik.Core` only. The .NET builds are trimming- and Native AOT-safe.
