---
status: accepted
date: 2026-10-30
decision-makers: milten89
---

# 0002. Result pattern and error handling

## Context and Problem Statement

Public Polish APIs fail in many routine ways: missing data (404 for a holiday), invalid requests (400), rate limits (429), outages (5xx), timeouts and broken connections. Consumers need to handle these predictably. Today some of them are returned as `OpenUrzednikResult` errors and others (network failures, timeouts, mapping errors) escape as exceptions, and the code base contains broad `catch (Exception)` blocks. What is the contract between "returned as a failed result" and "thrown"?

## Decision Drivers

* A consumer should be able to handle every expected failure without `try/catch`.
* Consumers preferring exceptions must still have a simple path (`EnsureSuccess()`).
* Real bugs and fatal conditions must not be hidden inside results.
* Caller-initiated cancellation must behave like everywhere else in .NET.

## Considered Options

* All failures that can happen during normal execution are returned in the result
* Only API responses (HTTP status, payload) are returned in the result; transport failures are thrown
* Replace the custom result with a third-party library or plain exceptions

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
| 5xx (504 included) | `ServiceUnavailableError` | `serviceUnavailable` | `statusCode` |
| A resilience handler's open circuit breaker or rate limiter (through the DI package, [ADR-0004](0004-dependency-policy.md)) | `ServiceUnavailableError`; `Exception` is an `HttpRequestException` whose `InnerException` is the rejection | `serviceUnavailable` | none |
| Any other non-success status | `UnknownError` | `unknown` | `statusCode` |
| Empty or malformed payload, deserialization or mapping failure | `SerializationError` (or a more specific error) | `serialization` | none |
| DNS, TLS or connection failure while sending (`HttpRequestException`); network failure while reading the body (`HttpRequestException`, `IOException`) | `ConnectionFailedError` with the cause in `Exception` | `connectionFailed` | none |
| Timeout: an `OperationCanceledException` while the **caller's** token is not cancelled (the request deadline, `HttpClient.Timeout`, a resilience handler's timeout) | `RequestTimeoutError`, with `Timeout` when the limit is known | `requestTimeout` | none |
| `default(OpenUrzednikResult<T>)` | `UnknownError` | `unknown` | none |

The metadata keys are public constants next to the errors that set them: `OpenUrzednikError.StatusCodeMetadataKey`, `RateLimitExceededError.RetryAfterMetadataKey`, `ValidationError.RuleNameMetadataKey`, `NameMetadataKey` and `ValueMetadataKey`. Other metadata, if any appears, is informational. A validator for a personal identifier (PESEL, NIP, REGON, bank account number) passes a masked value to `GetValidationErrorResult`, which puts it into the error's metadata ([ADR-0003](0003-telemetry-abstractions.md)).

**Compatibility of the error contract** (from the first stable release):

* Within a major version, the error type and `Code` of a documented condition don't change, and neither do the documented metadata keys.
* The set of errors is open: callers always handle a default case (e.g. treat any unknown error like `UnknownError`).
* A minor version may turn an `UnknownError` case into a more specific error, and may return a subclass of today's error type that keeps the parent's `Code` (unsealing a type is additive).

**Thrown:**

* Caller cancellation – `OperationCanceledException` when the caller's `CancellationToken` was cancelled.
* Programmer errors – `ArgumentNullException`, `ArgumentException`, invalid configuration detected in constructors/options. In the provider clients' public methods, a `null` argument (`ArgumentNullException`) and a value that isn't a defined enum member (`ArgumentOutOfRangeException`) are thrown **synchronously**, before the method returns its task, as the task-based asynchronous pattern says for usage errors: the method checks them, then returns the task of a private `async` method.
* Fatal runtime conditions – `OutOfMemoryException`, `StackOverflowException`, etc. are never caught.

**Rules for code:**

* Never write `catch (Exception)` or a bare `catch`. Catch the specific exception types listed above and convert them to errors. The rule is not dogmatic: if a new specific exception type is expected during normal execution, add it explicitly and document why. Example: the exceptions a resilience handler throws when it rejects a request are translated in the DI packages ([ADR-0004](0004-dependency-policy.md)).
* Mappers and other internal code must not throw on bad API data; they return a result or the caller converts a known exception type to an error.
* Every `OpenUrzednikError` implements `CreateException()` returning an `OpenUrzednikException`-derived type (the original exception, if any, becomes `InnerException`). The public, non-virtual `ToException()` calls it and attaches the error, so `EnsureSuccess()` always throws exceptions from the library's hierarchy that keep their error.
* The exception keeps the error it was created from (`OpenUrzednikException.Error`, with its metadata) and, when there were several, all of them (`Errors`).
* Several errors are produced only by validation. `EnsureSuccess()` then throws **one `OpenUrzednikValidationException` whose `Errors` lists every failure**, the pattern used by FluentValidation, never an `AggregateException`. If a result ever holds several errors of other kinds, the first error's exception is thrown, with all of them in `Errors`.
* `default(OpenUrzednikResult<T>)` must not be observable as a successful result: it is a failure with an `UnknownError`.
* `default(OpenUrzednikResult)` (the non-generic result) is a success on purpose: it has no value that could be uninitialized, and it is the same as `new OpenUrzednikResult()` and `OpenUrzednikResult.Success()`.
* `OpenUrzednikResult<T>.Value` on a failure throws `InvalidOperationException`, as `Nullable<T>.Value` does; the message lists each error's code and message, and the first error's exception is the `InnerException`. Callers read `Value` after `IsSuccess` or `TryGetValue`, or call `EnsureSuccess()` for a typed exception.

### Consequences

* Good, because consumers can write total handling code using `result.IsFailure` and error types only.
* Good, because `EnsureSuccess()` users get a consistent exception hierarchy.
* Bad, because the HTTP layer must carefully distinguish timeout from caller cancellation.
* Bad, because new expected exception types must be added deliberately.
* Bad, because it departs from the .NET Framework Design Guidelines, which report failures with exceptions. The departure is deliberate: for a public API these failures (a holiday without data, a rate limit, an outage) are normal flow, and a result works like the Try pattern the guidelines recommend for common failures, without a `try/catch` per call.
* Bad, because callers must check `IsFailure` (or call `EnsureSuccess()`); a forgotten check surfaces only when they read `Value`.
* A dual surface (a throwing and a result-returning method for each operation) was rejected: it doubles the API and its documentation; `EnsureSuccess()`/`EnsureSuccessAsync()` give the exception style on top of one API.

### Confirmation

* WireMock tests cover every error path for each client, asserting the error type: 400 with the server's message, 401 and 403, 404, 429 with `Retry-After`, 5xx, a 200 with an empty array (`NotFoundError`, for endpoints that return an array), malformed JSON, a timeout and a connection failure. This is the canonical list; `CLAUDE.md` and the `/new-endpoint` skill point to it. Unit tests cover the status mapping in the request executor.
* `reviewer` agent and code review reject `catch (Exception)`.

## More Information

Supersedes the informal rule in `CONTRIBUTING.md`. Shared HTTP behaviour is implemented once, see [ADR-0006](0006-shared-http-layer.md).

**2026-10-02 change.** `EnsureSuccess()` throws `AggregateException` for several errors, which is outside the library's hierarchy and contradicts the rule above; the multiple-errors rule now says what to throw instead (implemented with backlog item 18, together with the `Error`/`Errors` properties). The `default(OpenUrzednikResult<T>)` rule was implemented in #15. Implicit conversions from a value or an error to `OpenUrzednikResult<T>` were considered and rejected: C# doesn't apply user-defined conversions from interface types (e.g. `IReadOnlyList<T>`), and for `OpenUrzednikResult<object>` an error could silently become a successful value. Only `OpenUrzednikError` → non-generic `OpenUrzednikResult` is allowed.

**2026-10-02 change (backlog item 18).** The multiple-errors rule and `Error`/`Errors` are implemented. To guarantee that every exception keeps its error, `ToException()` is no longer abstract: errors implement the protected `CreateException()` (returning `OpenUrzednikException`, no longer `Exception`) and `ToException()` attaches the error. `OpenUrzednikResult` gained `Map`, `Bind`, `BindAsync`, `Match` and `TryGetValue`, which forward errors without copying them, and the implicit conversion from `OpenUrzednikError`.

**2026-10-04 decision (backlog item 36).** Results are not comparable: `OpenUrzednikResult` and `OpenUrzednikResult<T>` don't implement `IEquatable<>` or `==`, and the performance analyzer's CA1815 stays suppressed with that justification. Value equality would have to define when two failures are equal (same error instances? same codes?) and would invite comparing results instead of inspecting them. Callers compare `IsSuccess`, the value or the error codes.

**2026-10-04 change (backlog item 32).** `ValidationException` and `SerializationException` are renamed to `OpenUrzednikValidationException` and `OpenUrzednikSerializationException`: the old names clashed with `System.ComponentModel.DataAnnotations.ValidationException` and `System.Runtime.Serialization.SerializationException`. The .NET types themselves were rejected: they don't derive from `OpenUrzednikException` (so they can't carry `Error`/`Errors`), their meaning is different (attribute-based model validation, the obsolete formatter serialization), and `DataAnnotations` is an extra package on netstandard2.0.

**2026-10-17 change: one list of error paths** ([#77](https://github.com/milten89/OpenUrzednik.NET/issues/77)). The Confirmation, `CLAUDE.md` and the `/new-endpoint` skill listed different error paths: `CLAUDE.md` left out 401/403, and the skill also left out the connection failure. The Confirmation now lists every path the WireMock tests already cover for each NBP client, including the 200 with an empty array for endpoints that return one, and the other two documents point to it.

**2026-10-29 change: argument errors are thrown** ([#89](https://github.com/milten89/OpenUrzednik.NET/issues/89)). Before, the NBP clients returned a `ValidationError` for a `null` currency code and an undefined `TableType`, while the rules above and `CONTRIBUTING` named `ArgumentNullException` for a `null` argument, so callers couldn't tell from the docs whether to catch an exception or check the result. The maintainer drew the line where the .NET design guidelines do: a usage error (a `null` argument or an undefined enum value) throws, synchronously; a non-null value of the right type that fails a format or range check is a returned `ValidationError`. Core's extensions on `Task<…>` (`EnsureSuccessAsync`, `MapAsync`, `BindAsync`) still check their arguments inside an `async` method; that is a separate change. It affects the 15 methods that take a currency code or a `TableType`; the gold methods take neither. It is a breaking change for preview users.

**2026-10-29 change: the 1.0 error contract** ([#90](https://github.com/milten89/OpenUrzednik.NET/issues/90)). The "Returned" list became the mapping table above, and the compatibility rule, the reasons for departing from the design guidelines, the `default(OpenUrzednikResult)` rule and the `Value` rule were added. Changes made with it:

* Network failures were a `ServiceUnavailableError` without a status, the same type as a 5xx and a resilience handler's rejection. They are now a `ConnectionFailedError` (`connectionFailed`, `ConnectionFailedException`). The rejections stay a `ServiceUnavailableError`: the DI package marks them, and the executor tells them apart ([ADR-0004](0004-dependency-policy.md)).
* 408 was an `UnknownError`; it is now a `RequestTimeoutError` with `statusCode`. 504 stays a `ServiceUnavailableError`, because it is the gateway, not the client, that timed out.
* The codes with an `Error` suffix were renamed: `validationError` → `validation`, `serializationError` → `serialization`, `unknownError` → `unknown`.
* The metadata keys became public constants.
* `Value` on a failure named no error; its message now lists them.

These are breaking changes for preview users.

**2026-10-30 change: masked values in validation errors** ([#109](https://github.com/milten89/OpenUrzednik.NET/issues/109)). `ValueValidator<T>.GetValidationErrorResult` stores the validated value under `ValueMetadataKey`, so a validator for a personal identifier would copy the identifier into every error, exception and log that shows the metadata. A validator for a personal identifier must pass a masked value, under the rule in [ADR-0003](0003-telemetry-abstractions.md). NBP validates no such value, so nothing changes there.
