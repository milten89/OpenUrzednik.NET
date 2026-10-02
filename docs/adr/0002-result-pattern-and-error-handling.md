---
status: accepted
date: 2026-10-02
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

* Input validation failures → `ValidationError`.
* Every non-success HTTP status, mapped to the most specific error (404 → `NotFoundError`, 400 → validation/bad-request error carrying the server message, 401/403 → `UnauthorizedError`, 429 → `RateLimitExceededError` with `RetryAfter`, 5xx → `ServiceUnavailableError`, anything else → `UnknownError` with the status code in metadata).
* Empty or malformed payloads, deserialization and mapping failures → `SerializationError` (or a more specific error).
* Network failures (`HttpRequestException`, socket/IO errors).
* Timeouts – detected as `OperationCanceledException` while the **caller's** token is not cancelled.

**Thrown:**

* Caller cancellation – `OperationCanceledException` when the caller's `CancellationToken` was cancelled.
* Programmer errors – `ArgumentNullException`, `ArgumentException`, invalid configuration detected in constructors/options.
* Fatal runtime conditions – `OutOfMemoryException`, `StackOverflowException`, etc. are never caught.

**Rules for code:**

* Never write `catch (Exception)` or a bare `catch`. Catch the specific exception types listed above and convert them to errors. The rule is not dogmatic: if a new specific exception type is expected during normal execution, add it explicitly and document why. Example: the exceptions a resilience handler throws when it rejects a request are translated in the DI packages ([ADR-0004](0004-dependency-policy.md)).
* Mappers and other internal code must not throw on bad API data; they return a result or the caller converts a known exception type to an error.
* Every `OpenUrzednikError` implements `CreateException()` returning an `OpenUrzednikException`-derived type (the original exception, if any, becomes `InnerException`). The public, non-virtual `ToException()` calls it and attaches the error, so `EnsureSuccess()` always throws exceptions from the library's hierarchy that keep their error.
* The exception keeps the error it was created from (`OpenUrzednikException.Error`, with its metadata) and, when there were several, all of them (`Errors`).
* Several errors are produced only by validation. `EnsureSuccess()` then throws **one `ValidationException` whose `Errors` lists every failure**, the pattern used by FluentValidation, never an `AggregateException`. If a result ever holds several errors of other kinds, the first error's exception is thrown, with all of them in `Errors`.
* `default(OpenUrzednikResult<T>)` must not be observable as a successful result: it is a failure with an `UnknownError`.

### Consequences

* Good, because consumers can write total handling code using `result.IsFailure` and error types only.
* Good, because `EnsureSuccess()` users get a consistent exception hierarchy.
* Bad, because the HTTP layer must carefully distinguish timeout from caller cancellation.
* Bad, because new expected exception types must be added deliberately.

### Confirmation

* Unit and WireMock tests cover each status code, timeout, connection failure and malformed payload, asserting the error type.
* `reviewer` agent and code review reject `catch (Exception)`.

## More Information

Supersedes the informal rule in `CONTRIBUTING.md`. Shared HTTP behaviour is implemented once, see [ADR-0006](0006-shared-http-layer.md).

**2026-10-02 change.** `EnsureSuccess()` throws `AggregateException` for several errors, which is outside the library's hierarchy and contradicts the rule above; the multiple-errors rule now says what to throw instead (implemented with backlog item 18, together with the `Error`/`Errors` properties). The `default(OpenUrzednikResult<T>)` rule was implemented in #15. Implicit conversions from a value or an error to `OpenUrzednikResult<T>` were considered and rejected: C# doesn't apply user-defined conversions from interface types (e.g. `IReadOnlyList<T>`), and for `OpenUrzednikResult<object>` an error could silently become a successful value. Only `OpenUrzednikError` → non-generic `OpenUrzednikResult` is allowed.

**2026-10-02 change (backlog item 18).** The multiple-errors rule and `Error`/`Errors` are implemented. To guarantee that every exception keeps its error, `ToException()` is no longer abstract: errors implement the protected `CreateException()` (returning `OpenUrzednikException`, no longer `Exception`) and `ToException()` attaches the error. `OpenUrzednikResult` gained `Map`, `Bind`, `BindAsync`, `Match` and `TryGetValue`, which forward errors without copying them, and the implicit conversion from `OpenUrzednikError`.
