---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Errors ([ADR-0002](../../docs/adr/0002-result-pattern-and-error-handling.md))

- Throw only for caller cancellation (`OperationCanceledException` when the caller's token is cancelled), programmer errors (`ArgumentNullException`, invalid options) and fatal errors. In every public method of a public type that returns a `Task`, a `null` argument and an undefined enum value (`ArgumentOutOfRangeException`) throw **synchronously**: the method isn't `async` and checks them before it creates the task (typically it returns the task of a private `…CoreAsync` method). A non-null value of the right type that fails a format or range check is a returned `ValidationError`.
- A timeout is an `OperationCanceledException` while `cancellationToken.IsCancellationRequested == false`. Return it as an error.
- New error types derive from `OpenUrzednikError`, define `public const string ErrorCode`, and implement `CreateException()` returning an `OpenUrzednikException` subtype. The public `ToException()` calls it and attaches the error (`OpenUrzednikException.Error`/`Errors`).
- Error types describe conditions, not transports ([ADR-0021](../../docs/adr/0021-transport-subclasses-of-condition-errors.md)): `OpenUrzednikError` and the condition types have no transport members, and Core holds no transport types (except the obsolete `HttpErrorMetadata`, kept through 1.x).
  - A transport's public types live in `OpenUrzednik.Transport.<Transport>`, depending only on Core; its internals stay shared source.
  - An error caused by an HTTP response is the condition's subclass in `OpenUrzednik.Transport.Http` (`HttpNotFoundError : NotFoundError, IOpenUrzednikHttpStatusCode`, …, with a `System.Net.HttpStatusCode`; not sealed), read through `IOpenUrzednikHttpStatusCode` or `GetHttpStatus()` / `TryGetHttpStatus()`; an error with no response behind it (empty answer, client-side timeout, resilience rejection) is the plain condition type or a provider subclass of it.
  - A provider's own typed details (its API's error code, a proof id) are a sealed subclass in the provider package, named with its type prefix: derived from the HTTP subclass when a response caused the error (`WhiteListBadRequestError : HttpBadRequestError`), else from the condition type (`WhiteListNotFoundError : NotFoundError`); a detail several of its types carry is a provider detail interface (`IWhiteListErrorCode`), and its metadata keys are its own constants that never reuse a key of Core or the transport.
  - Never put a provider detail into Core or a transport package.
  - Detail interfaces are frozen: a new detail is a new interface.
  - A condition's own typed details are subclasses that keep its `Code`: `ValidationError<T>` (`Value`), `RangeValidationError<T>` (`Start`/`End`, for rules over two parameters; never a tuple).
  - Public dates in errors are `DateTime`, never `DateOnly` (ADR-0005).
  - The 1.0 `statusCode` constructors and `HttpErrorMetadata` are `[Obsolete]` until 2.0: never call them.
  - A new condition gets a new condition type, never a transport base class or interface.
- `Metadata` is the JSON-shaped view of an error's details (ADR-0021), built lazily: a type adds its keys in `protected override void CollectMetadata(...)`, not in its constructor.
  - The library's own keys hold only `string`, `bool`, `int`, `long`, `double`, `null` or arrays of one of these; `decimal` as an exact invariant string; dates, times and durations as ISO 8601 strings (`"2026-01-05"`, `"PT30S"`); enums by name unless their numbers carry a standard meaning (an HTTP status is an `int`); never tuples, ranges in one value, or other objects.
  - Keys are camelCase public constants.
  - The one exception until 2.0 is `retryAfter` (`TimeSpan`); `retryAfterDuration` is its ISO form.
  - `TestCommon`'s `ShouldFollowTheMetadataRule()` checks it in tests.
- Several errors (only validation produces them) are thrown by `EnsureSuccess()` as one `OpenUrzednikValidationException` listing all of them, never as `AggregateException`. Several errors of other kinds: the first error's exception, with all of them in `Errors`.
- Results are not comparable: don't add `Equals`/`==` to `OpenUrzednikResult` (CA1815 stays suppressed); compare `IsSuccess`, the value or error codes.
