---
status: superseded by ADR-0021
released-in: v1.0.0
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0012. Error types describe conditions, not transports

## Context and Problem Statement

The error types of [ADR-0002](0002-result-pattern-and-error-handling.md) (`NotFoundError`, `RateLimitExceededError`, `ServiceUnavailableError` and the others) are the contract callers branch on. Callers also need transport details, such as the HTTP status code, read in a typed way (#123). NBP is a REST provider, but the planned providers aren't all: GUS uses SOAP and has SOAP faults, and the MF flat file is a downloaded file with I/O failures. The same condition already comes from different sources: a `NotFoundError` is an HTTP 404 or an empty answer with status 200. Where do transport details belong, so that one error type can serve every provider?

## Decision Drivers

* Callers handle a condition (not found, rate limited, unavailable) the same way, whichever provider and transport produced it.
* `OpenUrzednikError` is the general base type, like `Exception`; it shouldn't know about HTTP.
* Public types are frozen from 1.0 ([ADR-0011](0011-compatibility-policy.md)): a type added now is hard to change later.
* A typed way to read the details, without a type check, and without allocating on the error path.

## Considered Options

* Transport details in metadata, with typed extension accessors in Core
* A transport base class for the condition types (`OpenUrzednikHttpError : OpenUrzednikError`, the seven HTTP-status types deriving from it)
* A transport interface on the condition types (`IOpenUrzednikHttpError { int? StatusCode }`)
* Transport subclasses of the condition types now (`HttpNotFoundError : NotFoundError`, and so on)

## Decision Outcome

Chosen option: "Transport details in metadata, with typed extension accessors in Core", because it keeps the condition types usable by every transport, keeps the base type general, and adds no type to the frozen hierarchy.

* `OpenUrzednikError` and the condition types are transport-agnostic. A provider maps its failures onto them, whatever the transport. A condition no existing type describes gets a new condition type (an additive change, [ADR-0002](0002-result-pattern-and-error-handling.md)), never a transport type.
* Transport details go into `OpenUrzednikError.Metadata`, under public constant keys, read through typed extension methods in `OpenUrzednik.Core.Errors`, one static class per transport. For HTTP: `HttpErrorMetadata.StatusCodeKey` (`"statusCode"`, an `int`), `TryGetHttpStatusCode(out int)` and `GetHttpStatusCode()` (`int?`). A provider with another transport adds its own class and keys (e.g. a SOAP fault code) in its ADR. The underlying exception, if any, stays in the error's `Exception` property, for the types that have one.
* The base type has no transport members: no status-code constructor parameter or constant. A condition type that can carry a status takes it as a constructor parameter and stores it under the transport's key.
* When a typed property is needed later, a minor version may add transport subclasses of the condition types (e.g. `HttpNotFoundError : NotFoundError`). They keep the parent's `Code` and keep writing the metadata, so code that checks the parent type, the code or the accessors keeps working ([ADR-0002](0002-result-pattern-and-error-handling.md) allows a subclass with the parent's `Code`; [ADR-0011](0011-compatibility-policy.md) allows unsealing in a minor version).

### Consequences

* Good, because a GUS SOAP "no entity" and an MF missing file can both be a `NotFoundError`, and callers handle them like NBP's.
* Good, because a transport's accessors can be added in any minor version: extension methods aren't frozen like interfaces or abstract members.
* Good, because reading a status code doesn't allocate: the accessor unboxes the stored `int`.
* Bad, because the status code isn't a property: callers use `GetHttpStatusCode()` or `TryGetHttpStatusCode` instead of `error.StatusCode`.

### Confirmation

* The tests check that every condition type with a status stores it under the key and that the accessors return it, and that errors without a response return none: validation, serialization and connection failure in Core's tests, a resilience rejection in the shared HTTP layer's tests.
* The `reviewer` agent flags a transport-typed condition error (a base class or interface named after a transport on a condition type) and a transport member on `OpenUrzednikError`.

## Pros and Cons of the Options

### A transport base class for the condition types

* Good, because the status is a typed property, stored once.
* Bad, because it makes condition types HTTP types: a `NotFoundError` from an empty answer, a client-side timeout or a resilience rejection would be an "HTTP error" with no response, and GUS and MF couldn't reuse the types.
* Bad, because it adds an abstract extension point that is frozen from 1.0.

### A transport interface on the condition types

* Good, because sealed types can implement it, and callers use a property pattern.
* Bad, because the same mismatch: the interface is declared by the type, but only some instances come from HTTP.
* Bad, because an interface can never gain a member after 1.0 ([ADR-0011](0011-compatibility-policy.md)).

### Transport subclasses of the condition types now

* Good, because typed and correct: only errors from HTTP are HTTP subclasses.
* Bad, because seven HTTP subclasses now and as many again per transport later, all frozen from 1.0, before anyone needs them. They stay possible later (above).

## More Information

Superseded by [ADR-0021](0021-transport-subclasses-of-condition-errors.md).

Decided with the maintainer on 2026-11-05 in #123. Related: [ADR-0002](0002-result-pattern-and-error-handling.md) (the error contract), [ADR-0006](0006-shared-http-layer.md) (the shared HTTP layer that maps responses to errors), [ADR-0010](0010-provider-readiness-gate.md) (provider ADRs, which name their transport's keys).

### Changes before 1.0

* 2026-11-05 · [#111](https://github.com/milten89/OpenUrzednik.NET/issues/111) · Wording only: `confidence` in the front matter, and the migration note for preview users removed (the release notes cover migrations). The [version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0012-error-types-describe-conditions.md).
