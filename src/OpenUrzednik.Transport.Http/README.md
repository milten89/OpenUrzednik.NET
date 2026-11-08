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

A provider may map a status to another condition (e.g. a quota answered with 400); the error is then that condition's HTTP subclass. Every subclass keeps its condition's `Code` and exception, so your `is NotFoundError` and `Code` checks handle an `HttpNotFoundError` like any other `NotFoundError`. A provider can derive its own type from an HTTP subclass to add details of its API, such as its error code; its README names those types, and they pass the same checks. Read the status through `IOpenUrzednikHttpStatusCode`:

```csharp
using System.Net;

using OpenUrzednik.Transport.Http;

var result = await client.GetAsync(date);
if (result.IsFailure && result.Errors[0] is IOpenUrzednikHttpStatusCode http)
    Console.WriteLine($"HTTP {(int)http.StatusCode}");

// Or, without a type check: null when no HTTP response caused the error.
HttpStatusCode? status = result.Errors[0].GetHttpStatus();
```

An error with no HTTP response behind it is Core's plain type (or a provider's subclass of it): an empty answer (`NotFoundError`), a client-side timeout (`RequestTimeoutError`), a resilience handler's rejection (`ServiceUnavailableError`) or a network failure (`ConnectionFailedError`).

The status is a `System.Net.HttpStatusCode`. 429 Too Many Requests (RFC 6585) has no named member on netstandard2.0 and .NET Framework, so compare with `(HttpStatusCode)429` there. `TryGetHttpStatus(out var status)` is the non-nullable form of `GetHttpStatus()`.

`Metadata` holds the status as an `int` under `"statusCode"` (`HttpStatusExtensions.StatusCodeMetadataKey`), the type OpenTelemetry uses for `http.response.status_code`, so you can copy `Metadata` into a ProblemDetails response as it is.

Core's `GetHttpStatusCode()` and `TryGetHttpStatusCode()` (`HttpErrorMetadata`) are obsolete: they keep working until 2.0, but use `GetHttpStatus()` from this package.

## Compatibility

`IOpenUrzednikHttpStatusCode` never gets a new member: a further HTTP detail comes as a separate interface. The HTTP error types stay unsealed, so providers can derive from them. Code that compares the exact type (`error.GetType() == typeof(NotFoundError)`, or `typeof(HttpNotFoundError)`, which a provider's subclass doesn't match) misses them; use `is` or `Code`.

## Dependencies

`OpenUrzednik.Core` only. The .NET builds are trimming- and Native AOT-safe.
