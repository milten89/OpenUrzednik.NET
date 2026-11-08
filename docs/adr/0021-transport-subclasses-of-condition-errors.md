---
status: accepted
date: 2026-11-08
confidence: high
decision-makers: milten89
---

# 0021. Error details: transport packages, typed subclasses and a JSON-shaped metadata view

## Context and Problem Statement

[ADR-0012](0012-error-types-describe-conditions.md) made the error types describe conditions (not found, rate limited, unavailable), whatever the transport, but let a condition type take an HTTP status code as a constructor parameter and stored it in `Metadata`, an untyped dictionary read by string key. So Core 1.0.0 shipped seven condition types with an HTTP parameter in their public signature (`NotFoundError(string message, int? statusCode = null)` and the others) and an HTTP accessor class (`HttpErrorMetadata`), which contradicts the rule they were meant to follow: a SOAP provider (GUS) or the White List flat file would see HTTP in Core. Only the shared HTTP layer passes a status; every other error leaves it `null`. `Metadata` also held whatever .NET value a type put there: a `TimeSpan`, NBP's `DateOnly`, or a `(DateOnly, DateOnly)` tuple, which logs and OpenTelemetry write with culture-dependent text and System.Text.Json writes as `{}`. Where do transport details live, how are typed details read, and what may `Metadata` hold?

## Decision Drivers

* Core is transport-agnostic: its condition types are used as they are by every transport (REST, SOAP, a downloaded file), and it holds no transport types.
* Code that handles a condition with `is NotFoundError`, a `Code` comparison or `HttpErrorMetadata`'s accessors keeps working within 1.x ([ADR-0011](0011-compatibility-policy.md)).
* Details are typed, read without string keys, and not boxed unless someone asks for them (CLAUDE.md "Performance").
* `Metadata` can be copied as it is into a ProblemDetails response, logs or OpenTelemetry attributes, and read from other languages, also under Native AOT.
* A new detail can be added without changing a released type or interface.
* An app that builds its own client in this style can create the same errors as the providers.

## Considered Options

* Transport packages with subclasses that implement detail interfaces, typed detail subclasses for a condition's own details, and `Metadata` as a lazily built, JSON-shaped view of them
* The same subclasses and interfaces in Core
* Transport subclasses with the details only in `Metadata` (the later option of ADR-0012)
* Keep the status-code constructor parameters (ADR-0012)
* An interface on the condition types themselves (`NotFoundError : IHasHttpStatusCode`)
* Any .NET value in `Metadata`, a range as a tuple or a bounds string (`"from/to"`, `"min/max"`, etc.)

## Decision Outcome

Chosen option: the first, because it takes the transport out of Core, gives typed details that only the errors that have them carry, lets a new detail come as a new interface or subclass, keeps every existing check working, and gives one serialization form that works everywhere.

**Core and the condition types**

* `OpenUrzednikError` and the condition types (`BadRequestError`, `UnauthorizedError`, `NotFoundError`, `RequestTimeoutError`, `RateLimitExceededError`, `ServiceUnavailableError`, `UnknownError`, `ValidationError`, `SerializationError`, `ConnectionFailedError`) have no transport members: no transport property and no transport constructor parameter. A provider maps its failures onto them, whatever the transport. A condition no existing type describes gets a new condition type (an additive change, [ADR-0002](0002-result-pattern-and-error-handling.md)), never a transport type, base class or interface.
* A condition type may get subclasses that add typed details: a transport's (below) or the condition's own (`ValidationError<T>`). They keep the condition's `Code` and exception type, so `is`, `Code` checks and `EnsureSuccess()` treat them like the condition. Such a condition type is designed for inheritance, so it isn't sealed: this applies ADR-0011's rule that classes not designed for inheritance are sealed, and its permission to remove `sealed` in a minor version. That covers `BadRequestError` to `UnknownError` and `ValidationError`; `SerializationError`, `ConnectionFailedError` and a new condition type stay sealed until one needs a subclass. `CreateException()` stays overridable, so a transport subclass can attach its transport's exception (e.g. a BCL `HttpRequestException`) as the `InnerException`; it returns its condition's exception type.
* Core holds no transport types. The one exception is `HttpErrorMetadata`, released in 1.0.0, which stays in Core, obsolete, through 1.x (below).
* The underlying exception, when there is one, stays in the error's `Exception` property (on the types that have it), whatever the transport.

**Validation details**

* `ValidationError` gains `RuleName` and `Name`. `ValidationError<T>` (sealed) carries the rejected value as `T Value`, unboxed; its exception boxes it only when created. The 1.0 constructor taking `object? value` stays, and its value is reachable only through `Metadata`, as in 1.0.
* A rule over two parameters is a `RangeValidationError` (abstract, with a `private protected` constructor, so only Core derives from it: it isn't an extension point) carrying `StartName` and `EndName`, and `RangeValidationError<T>` (sealed) with `T Start` and `T End`. Start and end name the lower and upper bound of any pair (`from`/`to`, `min`/`max`, etc.); the parameters' own names are in `StartName` and `EndName`. Its `Name` is `null`. A range is never a tuple, in a property or in `Metadata`.
* `ValueValidator<T>.GetValidationErrorResult(string message)` returns a `ValidationError<T>`; the overload taking an `object` value still returns a plain `ValidationError`.
* Public dates are `DateTime` on every target ([ADR-0005](0005-target-frameworks.md)), so a provider reports a date as `ValidationError<DateTime>` or `RangeValidationError<DateTime>`, never with its internal `DateOnly`.

**Metadata**

* `Metadata` is the serialization view of an error's details; the typed properties are the typed view. Every error type keeps its own keys in it, also for details it exposes as properties, so an app can copy `Metadata` into ProblemDetails, logs or traces without a type check.
* It is built on first read: `OpenUrzednikError` calls a `protected virtual CollectMetadata` hook, in which each type adds its keys, then the entries added with the 1.0 `AddMetadata`, and caches the result with `Interlocked.CompareExchange`. A value nobody reads is never converted or boxed. `AddMetadata` keeps its 1.0 behaviour, belongs in the constructor, and invalidates the cache; a build that overlapped an `AddMetadata` call isn't cached.
* Values (the library's own keys): `string`, `bool`, `int`, `long`, `double`, `null`, or arrays of one of these types (for lists of items of one kind). Smaller integers and `float` are widened, a `ulong` above `long.MaxValue` is a string. A `decimal` is an invariant string (`"1234.50"`): exact, while a JSON number is read as a `double` in most languages. A `char` and a `Guid` are strings. Dates, times and durations are ISO 8601 strings: `"2026-01-05"` (a date: midnight with no time zone, the library's public dates, or a `DateOnly`), `"2026-01-05T14:30:00"` (with the offset when the value has one, `Z` for UTC), `"14:30:00"` (a `TimeOnly`), `"PT30S"` (a duration, written by Core's own formatter as `System.Xml.XmlConvert` would, without loading `System.Xml`). An enum is its member name, unless its numbers carry a standard meaning, when the owning type writes its number (an HTTP status is an `int`, as OpenTelemetry's `http.response.status_code`). Tuples, ranges in any form, records, anonymous types, dictionaries and other objects are forbidden. Keys are camelCase public constants on the type. Identifiers stay masked ([ADR-0003](0003-telemetry-abstractions.md)).
* Core converts values in one internal place. The library's errors only produce convertible values; tests enforce it. A `ValidationError` value with no form (from the 1.0 `object` constructor or `ValidationError<T>`) stays in `Metadata` as passed, so an app's own `ValueValidator<T>`, which kept its `value` key in 1.0, still has it. A `RangeValidationError<T>` bound with no form is left out (the type is new; `Start` and `End` keep it). Entries added through `AddMetadata` are stored as passed: for app-defined errors the rule is a recommendation.
* New keys: `RateLimitExceededError.RetryAfterDurationMetadataKey` (`"retryAfterDuration"`, ISO duration, always present, `null` when unknown), `RequestTimeoutError.TimeoutMetadataKey` (`"timeout"`, the same form), and `RangeValidationError`'s `startName`, `start`, `endName`, `end`, in that order. The 1.0 `retryAfter` key keeps its `TimeSpan` through 1.x, the one exception to the value rule.

**Transport packages**

* A transport's public types live in a package of their own, `OpenUrzednik.Transport.<Transport>`, which depends only on Core; the providers that use the transport depend on it (ADR-0004 lets a provider depend on other OpenUrzednik packages). A transport's internal machinery stays shared source compiled into each provider (for HTTP, [ADR-0019](0019-shared-http-layer-tested-through-test-host.md)), never a package that providers reach through `InternalsVisibleTo`: NuGet resolves the highest version a dependency asks for, so an app can run an older provider against a newer package even with lockstep releases ([ADR-0008](0008-branching-versioning-and-release.md)). For HTTP, `src/Http/Http.props` brings the `ProjectReference` to `OpenUrzednik.Transport.Http`, so every REST provider and the shared layer's test-only hosts depend on it too. This public package holds only the error types, detail interfaces and accessors apps use, so it doesn't reopen ADR-0019's rejection of a public `OpenUrzednik.Http` package, which was about the executor's provider-only types. A new transport's package is decided in its provider ADR ([ADR-0016](0016-next-providers-and-package-naming.md)'s readiness checklist), following this pattern.
* An error caused by the transport is the transport subclass of its condition, named after the transport. For HTTP, in `OpenUrzednik.Transport.Http` (namespace `OpenUrzednik.Transport.Http`): `HttpBadRequestError`, `HttpUnauthorizedError`, `HttpNotFoundError`, `HttpRequestTimeoutError`, `HttpRateLimitExceededError`, `HttpServiceUnavailableError`, `HttpUnknownError`. Each is sealed, takes its details (for HTTP the status code, a `System.Net.HttpStatusCode`) in its constructor and keeps its condition's `Code`.
* Transport details are read through detail interfaces, one per detail, named after the detail and implemented only by that transport's subclasses. For HTTP: `IOpenUrzednikHttpStatusCode` with `HttpStatusCode StatusCode` (429 has no named member on netstandard2.0, so callers there compare with `(HttpStatusCode)429`). A detail interface is frozen from its release, like the telemetry interfaces ([ADR-0003](0003-telemetry-abstractions.md)): a further detail (e.g. response headers) comes as a separate interface that callers check with `is`, never as a new member ([ADR-0011](0011-compatibility-policy.md)).
* Each transport has extension accessors in one static class, on any error, built on its interfaces. For HTTP: `HttpStatusExtensions.GetHttpStatus()` (`HttpStatusCode?`) and `TryGetHttpStatus(out HttpStatusCode)`, with the key constant `StatusCodeMetadataKey` (`"statusCode"`). They read the interface and, until 2.0, fall back to the metadata key, which errors built with the obsolete constructors carry. Their names differ from Core's obsolete `GetHttpStatusCode()`, which an `OpenUrzednik.Core.Errors` import would otherwise make ambiguous (CS0121).
* The shared HTTP layer returns the HTTP subclass for every error caused by a response status, and a provider's `MapErrorAsync` does the same for the responses it maps (which may map a status to another condition, e.g. a quota answered with 400). An error with no response behind it is the plain condition type: an empty answer, a client-side timeout, a resilience handler's rejection, a network failure (`ConnectionFailedError`).

**1.x compatibility and the planned 2.0 change**

* The 1.0 constructors of the condition types that take a `statusCode` keep working in 1.x, marked `[Obsolete]` (pointing to the constructor without it and to the HTTP subclass). Library code never calls them; only the tests that check they still work do. One 1.0 call shape binds to an obsolete constructor without passing a status, `new RequestTimeoutError(message, exception: e)`: its replacement is `new RequestTimeoutError(message, null, e)` (an overload taking only the exception would make `new RequestTimeoutError(message, null)` ambiguous).
* `HttpErrorMetadata` is `[Obsolete]`, pointing to `GetHttpStatus()` and `TryGetHttpStatus()`. It keeps reading `"statusCode"`, which the HTTP subclasses write as an `int`.
* Planned for 2.0, to be decided by the 2.0 ADR that supersedes this one and ADR-0002 ([ADR-0001](0001-record-architecture-decisions.md)): the obsolete constructors and `HttpErrorMetadata` are removed, the accessors' metadata fallback with them, and so is the `TimeSpan` under `retryAfter`. `"statusCode"` stays in `Metadata` as an `int`.

### Consequences

* Good, because Core describes conditions only, the same for every transport.
* Good, because details are typed (`HttpNotFoundError.StatusCode`, `ValidationError<int>.Value`, `RangeValidationError<DateTime>.Start`) and only the errors that have them carry them.
* Good, because `Metadata` serializes the same way in JSON, logs and traces, can go into a ProblemDetails response as it is, and holds only types that need no registration beyond the primitives under Native AOT.
* Good, because nothing is converted or boxed for metadata unless someone reads it.
* Good, because a new detail is a new interface or subclass, so nothing released changes.
* Good, because `is NotFoundError`, `is ValidationError`, a `switch` on the condition type, `Code` and Core's accessors behave as in 1.0.
* Bad, because code that compares the exact type (`error.GetType() == typeof(NotFoundError)`, Shouldly's `ShouldBeOfType<ValidationError>()`) no longer matches an HTTP or a typed validation error ([ADR-0002](0002-result-pattern-and-error-handling.md) allows a subclass with the parent's `Code` in a minor version); the release notes of the version that ships it say so.
* Bad, because NBP's validation metadata changes form: a date `value` is an ISO string instead of a `DateOnly`. A range error loses the `name` and `value` keys, in `Metadata` and in its exception's `Data`, to `startName`/`start`/`endName`/`end`: ADR-0002 promises `name` and `value` "when known", and a range has no single name (`"date range"` was never a parameter name) and no single value with a metadata form. The value types weren't documented, so this fits a minor version; the release notes say so, and the typed properties hold the values.
* Bad, because the 1.0 `object` constructor of `ValidationError` now converts an app's value too: a `DateTime` becomes a string, an enum its name, a `byte` an `int`, a `float` a `double`, a `decimal` a string. So does an app's `ValueValidator<T>.GetValidationErrorResult(message)`, which now returns a `ValidationError<T>`. App code that casts `Metadata["value"]` back to its own type fails, and NBP's date errors put a `DateTime` into their exception's `Data["value"]` instead of a `DateOnly` on .NET; the release notes say so.
* Bad, because there is one more package to publish, document and keep AOT-safe, more public types frozen from their release, and each new transport adds its own.
* Bad, because until 2.0 `retryAfter` keeps a `TimeSpan` next to `retryAfterDuration`, and the HTTP accessors keep a metadata fallback.

### Confirmation

* Core's tests check the metadata forms of every value type through `ValidationError` and `ValidationError<T>`, the duration formatter against `XmlConvert` (edge values and thousands of seeded random ones), the lazy build (no collection before the first read, one cached result, `AddMetadata` after a read or during a build, concurrent first reads), `ValidationError<T>`, `RangeValidationError<T>` with its key order, `retryAfterDuration`, `timeout`, the obsolete constructors, and that every Core error type follows the value rule.
* The Native AOT smoke app reads `Metadata` of a typed validation error, a range error, a rate-limit error and a timeout, so the converter runs in the native binary (ADR-0014).
* `OpenUrzednik.Transport.Http.Tests` checks that each HTTP subclass is its condition with the condition's `Code` and exception type, implements `IOpenUrzednikHttpStatusCode`, holds the status as an `int` in `Metadata`, follows the value rule and is read by Core's obsolete accessors; and every path of `GetHttpStatus()` / `TryGetHttpStatus()`.
* The shared HTTP layer's tests check the exact HTTP type for each status, and the plain type for an error with no response, including a resilience rejection. NBP's tests check the error type and metadata of each validator and that they follow the value rule.
* The `reviewer` agent flags a transport member (a property or a constructor parameter) on `OpenUrzednikError` or a condition type, a new transport type in Core, an error caused by an HTTP response created as the plain condition type, a transport base class or interface on a condition type, a member added to a released detail interface, a metadata value outside the rule in the library's errors, a tuple or `DateOnly` in a public error, a call to an obsolete constructor or `HttpErrorMetadata` outside the tests that check them, and the removal of one before 2.0.

## Pros and Cons of the Options

### The same subclasses and interfaces in Core

* Good, because no new package, and Core's accessors could read the interface.
* Bad, because Core would hold every transport's types, which breaks the transport-agnostic Core this decision is for.

### Transport subclasses with the details only in `Metadata`

* Good, because no interface to freeze.
* Bad, because the details stay untyped (a string key, an `object` value), and storing an `int` boxes it on every HTTP error.

### Keep the status-code constructor parameters

* Good, because nothing changes.
* Bad, because the condition types keep an HTTP parameter, and the next transport would add another one.

### An interface on the condition types themselves

* Good, because a property pattern reads the status.
* Bad, because the interface is declared by the type, while only some instances come from HTTP (a `NotFoundError` from an empty answer has no status), and it puts a transport into Core.

### Any .NET value in `Metadata`, a range as a tuple or a bounds string (`"from/to"`, `"min/max"`, etc.)

* Good, because no conversion and no new types.
* Bad, because logs and OpenTelemetry write `TimeSpan`, `DateTime`, `DateOnly` and tuples with `ToString()` (US date order, and a tuple in the server's culture), System.Text.Json writes a tuple as `{}`, and Native AOT needs every runtime type registered for ProblemDetails; a bounds string (`"from/to"`, `"min/max"`, etc.) is still a tuple, which a consumer has to split.

## More Information

* Supersedes [ADR-0012](0012-error-types-describe-conditions.md), which listed transport subclasses as a later option and chose the constructor parameters and metadata for 1.0.
* Related: [ADR-0002](0002-result-pattern-and-error-handling.md) (the error contract), [ADR-0004](0004-dependency-policy.md) (dependencies), [ADR-0005](0005-target-frameworks.md) (public dates), [ADR-0019](0019-shared-http-layer-tested-through-test-host.md) (the shared HTTP layer that maps responses to errors; it stays internal shared source).
* Facts checked on 2026-11-08: OpenTelemetry's attribute value types and `http.response.status_code` (an int); how OpenTelemetry .NET's `TagWriter` and the JSON console logger write other types (`Convert.ToString` with the invariant culture); System.Text.Json's output for `TimeSpan`, `DateTime`, `DateOnly` and a tuple, and its refusal to serialize `object` values without registered types under AOT defaults (run locally on .NET 10); netstandard2.0's `HttpStatusCode` lacking `TooManyRequests` (its reference assembly).
* An accepted record isn't edited later ([ADR-0001](0001-record-architecture-decisions.md)): a changed decision gets a new record that supersedes this one.
