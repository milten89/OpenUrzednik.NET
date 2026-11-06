---
status: accepted
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0002. Result pattern and error handling

## Context and Problem Statement

Public Polish APIs fail in many routine ways: missing data (404 for a holiday), invalid requests (400), rate limits (429), outages (5xx), timeouts and broken connections. Consumers need to handle these predictably. Without a rule, some of them end up as `OpenUrzednikResult` errors while others (network failures, timeouts, mapping errors) escape as exceptions, and broad `catch (Exception)` blocks creep in. What is the contract between "returned as a failed result" and "thrown"?

## Decision Drivers

* A consumer should be able to handle every expected failure without `try/catch`.
* Consumers preferring exceptions must still have a simple path (`EnsureSuccess()`).
* Real bugs and fatal conditions must not be hidden inside results.
* Caller-initiated cancellation must behave like everywhere else in .NET.

## Considered Options

* All failures that can happen during normal execution are returned in the result
* Only API responses (HTTP status, payload) are returned in the result; transport failures are thrown
* Replace the custom result with a third-party library or plain exceptions

For parts of the result API:

* A dual surface: a throwing and a result-returning method for each operation
* Implicit conversions from a value or an error to `OpenUrzednikResult<T>`
* .NET's `ValidationException` and `SerializationException` as the exceptions of validation and serialization errors

## Decision Outcome

Chosen option: "All failures that can happen during normal execution are returned in the result".

**Returned as a failed `OpenUrzednikResult` (never thrown):**

| Condition | Error | `Code` | Metadata |
|---|---|---|---|
| An argument fails a format or range check (a currency code in the wrong format, an empty string, a date or top count out of range) | `ValidationError` | `validation` | `ruleName`; `name` and `value` when known |
| 400 | `BadRequestError`; the message includes up to 500 characters of the server's message | `badRequest` | `statusCode` |
| 401, 403 | `UnauthorizedError` | `unauthorized` | `statusCode` |
| 404 | `NotFoundError` | `notFound` | `statusCode` |
| 200 with an empty array, for endpoints that return an array (no data for the date or range) | `NotFoundError` | `notFound` | none: no `statusCode` |
| 408 | `RequestTimeoutError` | `requestTimeout` | `statusCode` |
| 429 | `RateLimitExceededError` with `RetryAfter` | `rateLimitExceeded` | `statusCode`, `retryAfter` |
| 5xx (504 included: it is the gateway, not the client, that timed out) | `ServiceUnavailableError` | `serviceUnavailable` | `statusCode` |
| A resilience handler's open circuit breaker or rate limiter (through the DI package, which marks these rejections so the executor tells them apart from network failures, [ADR-0004](0004-dependency-policy.md)) | `ServiceUnavailableError`; `Exception` is an `HttpRequestException` whose `InnerException` is the rejection | `serviceUnavailable` | none |
| Any other non-success status | `UnknownError` | `unknown` | `statusCode` |
| Empty or malformed payload, deserialization or mapping failure | `SerializationError` (or a more specific error) | `serialization` | none |
| DNS, TLS or connection failure while sending (`HttpRequestException`); network failure while reading the body (`HttpRequestException`, `IOException`) | `ConnectionFailedError` with the cause in `Exception` | `connectionFailed` | none |
| Timeout: an `OperationCanceledException` while the **caller's** token is not cancelled (the request deadline, `HttpClient.Timeout`, a resilience handler's timeout) | `RequestTimeoutError`, with `Timeout` when the limit is known | `requestTimeout` | none |
| `default(OpenUrzednikResult<T>)` | `UnknownError` | `unknown` | none |

An error caused by a non-success HTTP status carries that status code. The metadata keys are public constants: the transport's key next to its accessors (`HttpErrorMetadata.StatusCodeKey`, read with `GetHttpStatusCode()` or `TryGetHttpStatusCode`, [ADR-0012](0012-error-types-describe-conditions.md)), the others next to the errors that set them: `RateLimitExceededError.RetryAfterMetadataKey`, `ValidationError.RuleNameMetadataKey`, `NameMetadataKey` and `ValueMetadataKey`. Other metadata, if any appears, is informational. A validator for a personal identifier (PESEL, NIP, REGON, bank account number) passes a masked value to `GetValidationErrorResult`, which puts it into the error's metadata ([ADR-0003](0003-telemetry-abstractions.md)).

**Compatibility of the error contract** (from the first stable release):

* Within a major version, the error type and `Code` of a documented condition don't change, and neither do the documented metadata keys.
* The set of errors is open: callers always handle a default case (e.g. treat any unknown error like `UnknownError`).
* A minor version may turn an `UnknownError` case into a more specific error, and may return a subclass of a condition's error type that keeps the parent's `Code` (unsealing a type is additive).

**Thrown:**

* Caller cancellation: `OperationCanceledException` when the caller's `CancellationToken` was cancelled.
* Programmer errors: `ArgumentNullException`, `ArgumentException`, invalid configuration detected in constructors/options. In every public method of a public type that returns a task (the provider clients' methods, Core's extensions on `Task<…>` such as `EnsureSuccessAsync`, `MapAsync` and `BindAsync`), a `null` argument (`ArgumentNullException`) and a value that isn't a defined enum member (`ArgumentOutOfRangeException`) are thrown **synchronously**, before the method returns its task, as the task-based asynchronous pattern says for usage errors: the method isn't `async` and checks them before it creates the task, typically by returning the task of a private `async` method. The line between thrown and returned is where the .NET design guidelines draw it: a usage error throws; a non-null value of the right type that fails a format or range check is a returned `ValidationError`.
* Fatal runtime conditions: `OutOfMemoryException`, `StackOverflowException`, etc. are never caught.

**Rules for code:**

* Never write `catch (Exception)` or a bare `catch`. Catch the specific exception types listed above and convert them to errors. The rule is not dogmatic: if a new specific exception type is expected during normal execution, add it explicitly and document why. Example: the exceptions a resilience handler throws when it rejects a request are translated in the DI packages ([ADR-0004](0004-dependency-policy.md)).
* Mappers and other internal code must not throw on bad API data; they return a result or the caller converts a known exception type to an error.
* Every `OpenUrzednikError` implements the protected `CreateException()` returning an `OpenUrzednikException`-derived type (the original exception, if any, becomes `InnerException`). The public, non-virtual `ToException()` calls it and attaches the error, so `EnsureSuccess()` always throws exceptions from the library's hierarchy that keep their error.
* The exception keeps the error it was created from (`OpenUrzednikException.Error`, with its metadata) and, when there were several, all of them (`Errors`).
* Several errors are produced only by validation. `EnsureSuccess()` then throws **one `OpenUrzednikValidationException` whose `Errors` lists every failure**, the pattern used by FluentValidation, never an `AggregateException` (which is outside the library's hierarchy). If a result ever holds several errors of other kinds, the first error's exception is thrown, with all of them in `Errors`.
* The exceptions of validation and serialization errors are `OpenUrzednikValidationException` and `OpenUrzednikSerializationException`, named so they don't clash with `System.ComponentModel.DataAnnotations.ValidationException` and `System.Runtime.Serialization.SerializationException`.
* `default(OpenUrzednikResult<T>)` must not be observable as a successful result: it is a failure with an `UnknownError`.
* `default(OpenUrzednikResult)` (the non-generic result) is a success on purpose: it has no value that could be uninitialized, and it is the same as `new OpenUrzednikResult()` and `OpenUrzednikResult.Success()`.
* `OpenUrzednikResult<T>.Value` on a failure throws `InvalidOperationException`, as `Nullable<T>.Value` does; the message lists each error's code and message, and the first error's exception is the `InnerException`. Callers read `Value` after `IsSuccess` or `TryGetValue`, or call `EnsureSuccess()` for a typed exception.
* Results compose with `Map`, `Bind`, `BindAsync`, `Match` and `TryGetValue`, which forward errors without copying them.
* From a value or an error, the only implicit conversion is `OpenUrzednikError` → the non-generic `OpenUrzednikResult`. There is no implicit conversion from a value or an error to `OpenUrzednikResult<T>`. A failed `OpenUrzednikResult` converts implicitly to a failed `OpenUrzednikResult<T>` with the same errors, so a failure can be passed on from a method returning the generic result; converting a successful one throws `InvalidOperationException`, because it has no value.
* Results are not comparable: `OpenUrzednikResult` and `OpenUrzednikResult<T>` don't implement `IEquatable<>`, `Equals` overrides or `==`, and the performance analyzer's CA1815 stays suppressed with that justification. Value equality would have to define when two failures are equal (the same error instances? the same codes?) and would invite comparing results instead of inspecting them. Callers compare `IsSuccess`, the value or the error codes.

### Consequences

* Good, because consumers can write total handling code using `result.IsFailure` and error types only.
* Good, because `EnsureSuccess()` users get a consistent exception hierarchy.
* Bad, because the HTTP layer must carefully distinguish timeout from caller cancellation.
* Bad, because new expected exception types must be added deliberately.
* Bad, because it departs from the .NET Framework Design Guidelines, which report failures with exceptions. The departure is deliberate: for a public API these failures (a holiday without data, a rate limit, an outage) are normal flow, and a result works like the Try pattern the guidelines recommend for common failures, without a `try/catch` per call.
* Bad, because callers must check `IsFailure` (or call `EnsureSuccess()`); a forgotten check surfaces only when they read `Value`.

### Confirmation

* WireMock tests cover every error path for each client, asserting the error type: 400 with the server's message, 401 and 403, 404, 429 with `Retry-After`, 5xx, a 200 with an empty array (`NotFoundError`, for endpoints that return an array), malformed JSON, a timeout and a connection failure. This is the canonical list; `CLAUDE.md` and the `/new-endpoint` skill point to it. Unit tests cover the status mapping in the request executor.
* The analyzer rule CA1031 (do not catch general exception types) is a warning in `src/` (`src/.editorconfig`), which CI turns into an error; the `reviewer` agent and code review reject `catch (Exception)` too. Tests aren't covered, since they may catch broadly on purpose.

## Pros and Cons of the Options

### A dual surface: a throwing and a result-returning method for each operation

* Bad, because it doubles the API and its documentation; `EnsureSuccess()`/`EnsureSuccessAsync()` give the exception style on top of one API.

### Implicit conversions from a value or an error to `OpenUrzednikResult<T>`

* Bad, because C# doesn't apply user-defined conversions from interface types (e.g. `IReadOnlyList<T>`).
* Bad, because for `OpenUrzednikResult<object>` an error could silently become a successful value.

### .NET's `ValidationException` and `SerializationException` as the exceptions of validation and serialization errors

* Bad, because they don't derive from `OpenUrzednikException`, so they can't carry `Error`/`Errors`.
* Bad, because their meaning is different: attribute-based model validation, and the obsolete formatter serialization.
* Bad, because `DataAnnotations` is an extra package on netstandard2.0.

## More Information

Supersedes the informal rule in `CONTRIBUTING.md`. Shared HTTP behaviour is implemented once, see [ADR-0006](0006-shared-http-layer.md). Error types describe conditions, not transports: [ADR-0012](0012-error-types-describe-conditions.md).

### Changes before 1.0

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0002-result-pattern-and-error-handling.md#more-information).

* 2026-10-02 · Several errors throw one `OpenUrzednikValidationException` instead of an `AggregateException`; implicit conversions from a value or an error to `OpenUrzednikResult<T>` were rejected.
* 2026-10-02 · backlog item 18 · Errors implement `CreateException()` and `ToException()` attaches the error (`Error`/`Errors`); `Map`, `Bind`, `BindAsync`, `Match`, `TryGetValue` and the conversion from `OpenUrzednikError` were added.
* 2026-10-04 · backlog item 36 · Results are not comparable.
* 2026-10-04 · backlog item 32 · The exceptions became `OpenUrzednikValidationException` and `OpenUrzednikSerializationException`; the .NET types were rejected.
* 2026-10-17 · [#77](https://github.com/milten89/OpenUrzednik.NET/issues/77) · One canonical list of error paths, in the Confirmation.
* 2026-10-29 · [#89](https://github.com/milten89/OpenUrzednik.NET/issues/89) · A `null` argument or an undefined enum value in a provider client throws synchronously instead of returning a `ValidationError`.
* 2026-10-29 · [#90](https://github.com/milten89/OpenUrzednik.NET/issues/90) · The 1.0 error contract: the mapping table, `ConnectionFailedError`, 408 as `RequestTimeoutError`, codes without the `Error` suffix, public metadata keys, the compatibility rule, and the `default` and `Value` rules.
* 2026-10-30 · [#109](https://github.com/milten89/OpenUrzednik.NET/issues/109) · Validators for personal identifiers put a masked value into the error's metadata.
* 2026-10-30 · [#159](https://github.com/milten89/OpenUrzednik.NET/issues/159) · Synchronous argument errors cover every public task-returning method of a public type, Core's `Task<…>` extensions included.
* 2026-10-30 · [#110](https://github.com/milten89/OpenUrzednik.NET/issues/110) · CA1031 enforces the rule against `catch (Exception)` in `src/`.
* 2026-11-05 · [#123](https://github.com/milten89/OpenUrzednik.NET/issues/123) · The status code is read with the typed accessors of `HttpErrorMetadata` ([ADR-0012](0012-error-types-describe-conditions.md)).
