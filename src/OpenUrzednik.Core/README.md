# OpenUrzednik.Core

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.pl.md)

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

A client returns these errors in the result. Switch on the type, or compare `Code` with the `ErrorCode` constants:

| Situation | Error | `Code` | Metadata |
|---|---|---|---|
| An argument fails a format or range check | `ValidationError` | `validation` | `ruleName`; `name` and `value` when known |
| HTTP 400 (the message includes the server's message) | `BadRequestError` | `badRequest` | `statusCode` |
| HTTP 401, 403 | `UnauthorizedError` | `unauthorized` | `statusCode` |
| HTTP 404 | `NotFoundError` | `notFound` | `statusCode` |
| No data for the date or range (an empty list from the API) | `NotFoundError` | `notFound` | none |
| HTTP 408 | `RequestTimeoutError` | `requestTimeout` | `statusCode` |
| HTTP 429 | `RateLimitExceededError` (`RetryAfter`) | `rateLimitExceeded` | `statusCode`, `retryAfter` |
| HTTP 5xx | `ServiceUnavailableError` | `serviceUnavailable` | `statusCode` |
| A resilience handler's open circuit breaker or rate limiter (with `AddOpenUrzednik…` DI registration) | `ServiceUnavailableError` | `serviceUnavailable` | none |
| Another HTTP status | `UnknownError` | `unknown` | `statusCode` |
| An empty or malformed response | `SerializationError` | `serialization` | none |
| No connection (DNS, TLS, refused or broken connection) | `ConnectionFailedError` | `connectionFailed` | none |
| A timeout (the client's or `HttpClient`'s deadline, a resilience handler's timeout) | `RequestTimeoutError` (`Timeout` when known) | `requestTimeout` | none |

The metadata keys are constants: `OpenUrzednikError.StatusCodeMetadataKey`, `RateLimitExceededError.RetryAfterMetadataKey`, `ValidationError.RuleNameMetadataKey`, `NameMetadataKey` and `ValueMetadataKey`.

From the first stable release, within a major version the type and `Code` of each case above, and the metadata keys, stay the same. New errors may appear in a minor version (e.g. a more specific error in place of `UnknownError`), so always handle a default case.

A client throws instead of returning an error in three cases: you cancel (`OperationCanceledException`), you pass an invalid argument (`ArgumentNullException`, `ArgumentOutOfRangeException`, thrown before the task is returned) or invalid options (`ArgumentException` from the constructor).
