# OpenUrzednik.Core

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Core/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by Narodowy Bank Polski (NBP) or any other Polish public institution.

Shared abstractions for the whole **OpenUrzednik.NET** set: the Result pattern (`OpenUrzednikResult` / `OpenUrzednikResult<T>`), typed errors (`OpenUrzednikError`) and exceptions (`OpenUrzednikException`) for consumers who prefer the classic style.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Core
```

## Example

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;

OpenUrzednikResult<int> result = OpenUrzednikResult.Success(42);
int value = result.EnsureSuccess();
```

A failed result:

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

OpenUrzednikResult<int> failed = OpenUrzednikResult.Failure<int>(new ValidationError("NIP is invalid.", ruleName: "nipChecksum", name: "nip", value: null));

try
{
    int value = failed.EnsureSuccess();
}
catch (OpenUrzednikValidationException ex)
{
    Console.WriteLine(ex.Message);
}
```

## Trimming and Native AOT

The .NET builds (`net8.0`, `net10.0`) are trimming- and Native AOT-compatible: publishing an app with trimming or Native AOT gives no warning from this package. CI checks it on every change ([ADR-0014](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0014-trimming-and-native-aot.md)). The `netstandard2.0` build (.NET Framework) isn't covered.

## Working with a result

```csharp
var label = result
    .Map(v => v * 2)
    .Match(v => $"Value: {v}", errors => $"Error: {errors[0].Message}");

if (result.TryGetValue(out var current))
    Console.WriteLine(current);
```

`Map` changes the value of a successful result, `Bind` runs another operation that also returns a result, and `Match` handles both cases. If the result holds errors, they pass through unchanged and the functions you pass aren't called.

`EnsureSuccess()` throws an exception derived from `OpenUrzednikException`. Its `Error` property holds the error with its metadata (e.g. the HTTP status code), and `Errors` holds all of the result's errors. Several validation errors go into one `OpenUrzednikValidationException`.

Read `Value` only after `IsSuccess` or `TryGetValue`. On a failed result it throws an `InvalidOperationException` that names the errors; use `EnsureSuccess()` if you want the typed exception.

Every provider package (today `OpenUrzednik.Nbp`) depends on this package. You usually don't install it on its own, unless you are building your own client in the same style.

## Errors

A client returns these errors in the result. Switch on the type, or compare `Code` with the `ErrorCode` constants. The types describe the situation, whatever the transport, so the same type can come from an HTTP response, a SOAP fault or an empty answer:

| Situation | Error | `Code` | Metadata |
|---|---|---|---|
| An argument fails a format or range check | `ValidationError` (`ValidationError<T>` with the value's type; `RangeValidationError<T>` for a range of two parameters) | `validation` | `ruleName`; `name` and `value` when known; `startName`, `start`, `endName`, `end` for a range |
| The service rejected the request (the message includes the service's message) | `BadRequestError` | `badRequest` | none in Core |
| The service denied access | `UnauthorizedError` | `unauthorized` | none in Core |
| The service has no such resource, or no data for the date or range | `NotFoundError` | `notFound` | none in Core |
| The service is rate limiting the caller | `RateLimitExceededError` (`RetryAfter`) | `rateLimitExceeded` | `retryAfter`, `retryAfterDuration` |
| The service is unavailable, or a resilience handler's open circuit breaker or rate limiter rejected the request | `ServiceUnavailableError` | `serviceUnavailable` | none in Core |
| The service answered with something no other error describes | `UnknownError` | `unknown` | none in Core |
| An empty or malformed response | `SerializationError` | `serialization` | none |
| No connection (DNS, TLS, refused or broken connection) | `ConnectionFailedError` | `connectionFailed` | none |
| A timeout (the client's or `HttpClient`'s deadline, a resilience handler's timeout, or the service said so) | `RequestTimeoutError` (`Timeout` when known) | `requestTimeout` | `timeout` |

When an HTTP response caused the error, a REST client returns the condition's subclass from [OpenUrzednik.Transport.Http](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Transport.Http/README.md) (e.g. `HttpNotFoundError`, a `NotFoundError`), which every REST provider brings; its README lists which status maps to which error, and how to read the status. `is NotFoundError` and `Code` checks match those subclasses too. The constructors of the condition types that take a `statusCode` are obsolete and will be removed in 2.0, and so is `HttpErrorMetadata`: use the transport package's types and `GetHttpStatus()`.

**Metadata.** `Metadata` holds the error's details in a form that serializes the same way in JSON, in logs and in OpenTelemetry attributes, so you can copy it into a ProblemDetails response as it is. The error types' own keys hold strings, booleans, numbers (`int`, `long`, `double`), `null` or arrays of one of these; a `decimal` is an exact invariant string (`"1234.50"`); dates, times and durations are ISO 8601 strings (`"2026-01-05"`, `"2026-01-05T14:30:00+01:00"`, `"PT30S"`), and enums their member name, except those whose numbers carry a standard meaning (an HTTP status is its number). Tuples and other objects never appear: a range has its own keys. One exception until 2.0: `retryAfter` is a `TimeSpan` (use `retryAfterDuration`). The typed values are on the error's properties (`RetryAfter`, `ValidationError<T>.Value`, `RangeValidationError<T>.Start`). `Metadata` is built when you first read it. Copying it into your own API's response passes on the details of the API behind it, such as its HTTP status: decide whether your clients should see them.

The metadata keys are constants: `ValidationError.RuleNameMetadataKey`, `NameMetadataKey` and `ValueMetadataKey`; `RangeValidationError.StartNameMetadataKey`, `StartMetadataKey`, `EndNameMetadataKey` and `EndMetadataKey`; `RateLimitExceededError.RetryAfterMetadataKey` and `RetryAfterDurationMetadataKey`; `RequestTimeoutError.TimeoutMetadataKey`; and, for HTTP, `HttpStatusExtensions.StatusCodeMetadataKey` in OpenUrzednik.Transport.Http.

From the first stable release, within a major version the type of each case above (or a subclass of it with the same `Code`, so `is` checks keep matching), its `Code` and the metadata keys stay the same. New errors may appear in a minor version (e.g. a more specific error in place of `UnknownError`), so always handle a default case.

A client throws instead of returning an error in three cases: you cancel (`OperationCanceledException`), you pass an invalid argument (`ArgumentNullException`, `ArgumentOutOfRangeException`, thrown before the task is returned) or invalid options (`ArgumentException` from the constructor).
