---
status: accepted
date: 2026-11-08
confidence: high
decision-makers: milten89
---

# 0021. Transport details in transport packages: subclasses and interfaces of the condition errors

## Context and Problem Statement

[ADR-0012](0012-error-types-describe-conditions.md) made the error types describe conditions (not found, rate limited, unavailable), whatever the transport, but let a condition type take an HTTP status code as a constructor parameter and stored it in `Metadata`, an untyped dictionary read by string key. So Core 1.0.0 shipped seven condition types with an HTTP parameter in their public signature (`NotFoundError(string message, int? statusCode = null)` and the others) and an HTTP accessor class (`HttpErrorMetadata`), which contradicts the rule they were meant to follow: a SOAP provider (GUS) or the White List flat file would see HTTP in Core. Only the shared HTTP layer passes a status; every other error leaves it `null`. Where do transport details live, and how are they read, without Core knowing any transport?

## Decision Drivers

* Core is transport-agnostic: its condition types are used as they are by every transport (REST, SOAP, a downloaded file), and it holds no transport types.
* Code that handles a condition with `is NotFoundError`, a `Code` comparison or `HttpErrorMetadata`'s accessors keeps working within 1.x ([ADR-0011](0011-compatibility-policy.md)).
* Transport details are typed, read without string keys or boxing, and a new detail can be added without changing a released type or interface.
* An app that builds its own client in this style can create the same errors as the providers.

## Considered Options

* A package per transport with subclasses of the condition types that implement detail interfaces (`OpenUrzednik.Transport.Http`: `HttpNotFoundError : NotFoundError, IOpenUrzednikHttpStatusCode`)
* The same subclasses and interfaces in Core
* Transport subclasses with the details in `Metadata` (the later option of ADR-0012)
* Keep the status-code constructor parameters (ADR-0012)
* An interface on the condition types themselves (`NotFoundError : IHasHttpStatusCode`)

## Decision Outcome

Chosen option: "A package per transport with subclasses of the condition types that implement detail interfaces", because it takes the transport out of Core, gives typed details that only errors from that transport carry, lets a new detail come as a new interface, and keeps every existing check working.

**Core and the condition types**

* `OpenUrzednikError` and the condition types (`BadRequestError`, `UnauthorizedError`, `NotFoundError`, `RequestTimeoutError`, `RateLimitExceededError`, `ServiceUnavailableError`, `UnknownError`, `ValidationError`, `SerializationError`, `ConnectionFailedError`) have no transport members: no transport property and no transport constructor parameter. A provider maps its failures onto them, whatever the transport. A condition no existing type describes gets a new condition type (an additive change, [ADR-0002](0002-result-pattern-and-error-handling.md)), never a transport type, base class or interface.
* Core holds no transport types. The one exception is `HttpErrorMetadata`, released in 1.0.0, which stays in Core through 1.x (below).
* The seven condition types from `BadRequestError` to `UnknownError` are designed for inheritance by transport subclasses, so they are not sealed: this applies ADR-0011's rule that classes not designed for inheritance are sealed, and its permission to remove `sealed` in a minor version. Their `CreateException()` stays overridable, so a transport subclass can attach its transport's exception (e.g. a BCL `HttpRequestException`) as the `InnerException`; it returns its condition's exception type. The other condition types, and a new one, stay sealed until a transport subclass needs them.
* The underlying exception, when there is one, stays in the error's `Exception` property (on the types that have it), whatever the transport.

**Transport packages**

* A transport's public types live in a package of their own, `OpenUrzednik.Transport.<Transport>`, which depends only on Core; the providers that use the transport depend on it (ADR-0004 lets a provider depend on other OpenUrzednik packages). A transport's internal machinery stays where its ADR puts it (for HTTP, the shared source of [ADR-0019](0019-shared-http-layer-tested-through-test-host.md)). For HTTP, `src/Http/Http.props` brings the `ProjectReference` to `OpenUrzednik.Transport.Http`, so every REST provider and the shared layer's test-only hosts depend on it too. This public package holds only the error types and detail interfaces apps use, so it doesn't reopen ADR-0019's rejection of a public `OpenUrzednik.Http` package, which was about the executor's provider-only types. A new transport's package is decided in its provider ADR ([ADR-0016](0016-next-providers-and-package-naming.md)'s readiness checklist), following this pattern.
* An error caused by the transport is the transport subclass of its condition, named after the transport. For HTTP, in `OpenUrzednik.Transport.Http` (namespace `OpenUrzednik.Transport.Http`): `HttpBadRequestError`, `HttpUnauthorizedError`, `HttpNotFoundError`, `HttpRequestTimeoutError`, `HttpRateLimitExceededError`, `HttpServiceUnavailableError`, `HttpUnknownError`. Each is sealed, takes its details (for HTTP the status code, an `int`, required) in its constructor and keeps its condition's `Code`.
* Transport details are read through detail interfaces, one per detail, named after the detail and implemented only by that transport's subclasses. For HTTP: `IOpenUrzednikHttpStatusCode` with `int StatusCode`. A detail interface is frozen from its release, like the telemetry interfaces ([ADR-0003](0003-telemetry-abstractions.md)): a further detail (e.g. response headers) comes as a separate interface that callers check with `is`, never as a new member ([ADR-0011](0011-compatibility-policy.md)).
* The shared HTTP layer returns the HTTP subclass for every error caused by a response status, and a provider's `MapErrorAsync` does the same for the responses it maps (which may map a status to another condition, e.g. a quota answered with 400). An error with no response behind it is the plain condition type: an empty answer, a client-side timeout, a resilience handler's rejection, a network failure (`ConnectionFailedError`).

**1.x compatibility and the planned 2.0 change**

* `HttpErrorMetadata` (`StatusCodeKey` = `"statusCode"`, an `int`; `GetHttpStatusCode()` and `TryGetHttpStatusCode(out int)`) stays in Core, unchanged, through 1.x, and is the way to read the status without a type check. Core can't see the transport's interface, so in 1.x the HTTP subclasses also store their status under `StatusCodeKey`, and the accessors keep reading it from there.
* The 1.0 constructors of the condition types that take a `statusCode` keep working in 1.x, marked `[Obsolete]` (pointing to the constructor without it and to the HTTP subclass). Library code never calls them; only the tests that check they still work do. One 1.0 call shape binds to an obsolete constructor without passing a status, `new RequestTimeoutError(message, exception: e)`: its replacement is `new RequestTimeoutError(message, null, e)` (an overload taking only the exception would make `new RequestTimeoutError(message, null)` ambiguous).
* Planned for 2.0, to be decided by the 2.0 ADR that supersedes this one and ADR-0002 ([ADR-0001](0001-record-architecture-decisions.md)): the obsolete constructors are removed; the accessors move to `OpenUrzednik.Transport.Http` and read `IOpenUrzednikHttpStatusCode`; the status leaves `Metadata`, and with it `HttpErrorMetadata` leaves Core.

### Consequences

* Good, because Core describes conditions only, the same for every transport.
* Good, because the status is typed (`HttpNotFoundError.StatusCode`, `is IOpenUrzednikHttpStatusCode http`) and only errors from HTTP have it.
* Good, because a new detail (of HTTP or another transport) is a new interface, so nothing released changes.
* Good, because `is NotFoundError`, a `switch` on the condition type, `Code` and the accessors behave as in 1.0.
* Bad, because code that compares the exact type (`error.GetType() == typeof(NotFoundError)`, Shouldly's `ShouldBeOfType<NotFoundError>()`) no longer matches an error caused by a response ([ADR-0002](0002-result-pattern-and-error-handling.md) allows a subclass with the parent's `Code` in a minor version); the release notes of the version that ships it say so.
* Bad, because there is one more package to publish, document and keep AOT-safe, and each new transport adds its own, with types frozen from their release.
* Bad, because until 2.0 the status is stored twice (the property and the metadata key), and reading it without a type check still goes through the untyped metadata.

### Confirmation

* Core's tests check that the constructors without a status carry none, that the obsolete constructors still store it, and that errors with no response (validation, serialization, connection failure) return none from the accessors.
* `OpenUrzednik.Transport.Http.Tests` checks that each HTTP subclass is its condition with the condition's `Code` and exception type, implements `IOpenUrzednikHttpStatusCode` with its status, and stores it for Core's accessors.
* The shared HTTP layer's tests check the exact HTTP type for each status, and the plain type for an error with no response, including a resilience rejection.
* The `reviewer` agent flags a transport member (a property or a constructor parameter) on `OpenUrzednikError` or a condition type, a new transport type in Core, an error caused by an HTTP response created as the plain condition type, a transport base class or interface on a condition type, a member added to a released detail interface, a call to an obsolete constructor outside the tests that check it, and the removal of one before 2.0.

## Pros and Cons of the Options

### The same subclasses and interfaces in Core

* Good, because no new package, and Core's accessors could read the interface.
* Bad, because Core would hold every transport's types, which breaks the transport-agnostic Core this decision is for.

### Transport subclasses with the details in `Metadata`

* Good, because no interface to freeze.
* Bad, because the details stay untyped (a string key, an `object` value), and storing an `int` boxes it on every HTTP error.

### Keep the status-code constructor parameters

* Good, because nothing changes.
* Bad, because the condition types keep an HTTP parameter, and the next transport would add another one.

### An interface on the condition types themselves

* Good, because a property pattern reads the status.
* Bad, because the interface is declared by the type, while only some instances come from HTTP (a `NotFoundError` from an empty answer has no status), and it puts a transport into Core.

## More Information

* Supersedes [ADR-0012](0012-error-types-describe-conditions.md), which listed transport subclasses as a later option and chose the constructor parameters and metadata for 1.0.
* Related: [ADR-0002](0002-result-pattern-and-error-handling.md) (the error contract), [ADR-0004](0004-dependency-policy.md) (dependencies), [ADR-0019](0019-shared-http-layer-tested-through-test-host.md) (the shared HTTP layer that maps responses to errors; it stays internal shared source).
* An accepted record isn't edited later ([ADR-0001](0001-record-architecture-decisions.md)): a changed decision gets a new record that supersedes this one.
