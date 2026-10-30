---
status: accepted
date: 2026-10-30
decision-makers: milten89
---

# 0011. Compatibility policy

## Context and Problem Statement

From 1.0 the packages follow SemVer 2 ([ADR-0008](0008-branching-versioning-and-release.md)): breaking changes only in a major version. SemVer doesn't say what counts as the API of a .NET library: which members, which behaviour, which targets, what a minor version may add. Users who mock, decorate or derive from our types, deconstruct our models, or filter our logs need to know what stays stable. What does a minor version promise, and how do we keep room to grow?

## Decision Drivers

* Users can upgrade within a major version without code changes, except where a rule below says so.
* The library can still add features in minor versions: client methods, model properties, options.
* The rules are checkable in review and, where possible, by tooling (package validation).
* The common .NET conventions (Framework Design Guidelines, the .NET breaking-change rules) apply where they fit.

## Considered Options

* An explicit scope, with client interfaces that may grow and sealed classes
* Frozen interfaces, with new methods as default interface members or as new interfaces
* No written policy: SemVer as commonly understood

## Decision Outcome

Chosen option: "An explicit scope, with client interfaces that may grow and sealed classes". The rules apply from the first stable release; previews may break anything.

**SemVer scope.**

* Covered within a major version: the public API on every target ([ADR-0005](0005-target-frameworks.md)), documented behaviour, the error contract (error types, codes and documented metadata keys, [ADR-0002](0002-result-pattern-and-error-handling.md)), and the telemetry contract (source and span names, documented tags, `error.type` values, logger categories and event ids, [ADR-0003](0003-telemetry-abstractions.md)). Those ADRs hold the full lists.
* Not covered: log and exception message text, log properties, the `error` and `exception` span events and span status descriptions ([ADR-0003](0003-telemetry-abstractions.md)), undocumented behaviour, internal types (including the shared HTTP layer, [ADR-0006](0006-shared-http-layer.md)), and previews. One exception: Core's internal members that other packages call through `InternalsVisibleTo` are only added to within a major, because an app may pair a newer Core with older packages ([ADR-0003](0003-telemetry-abstractions.md)).
* A minor version may: add public types and non-abstract members (never an abstract member on an extension point below, never a member on an interface other than a client interface); add methods to client interfaces; add non-`required` `init` properties to models, and options to `NbpOptions` that are optional and keep today's behaviour when unset; raise dependency minimums ([ADR-0004](0004-dependency-policy.md)); drop a .NET target that isn't the lowest one, once Microsoft has ended its support ([ADR-0005](0005-target-frameworks.md)); turn an `UnknownError` case into a more specific error ([ADR-0002](0002-result-pattern-and-error-handling.md)); remove `sealed`.
* Only a major version may: remove or change a public member; change documented behaviour, an error type or code, or a telemetry name; drop the lowest .NET target or netstandard2.0; seal a class; change the assembly version, which follows the major version ([ADR-0005](0005-target-frameworks.md)). The strong-name key never changes.
* A patch version adds no public API: it fixes bugs and security issues, and may raise a dependency to a patch version that fixes a vulnerability.

**Interfaces.**

* Client interfaces (`INbpGoldPriceClient`, `INbpCurrencyExchangeRateClient`, `INbpExchangeRateTableClient`, and those of future providers) may gain methods in minor versions; their XML docs say so. Only the user calls these methods, so a user's own implementation stays correct with a `throw new NotImplementedException();` stub for a method it doesn't use. It has to be rebuilt against the new version, though:

  | The user's code | After a minor version adds a method |
  |---|---|
  | calls the client | nothing changes; the new method is available |
  | mocks it (`Substitute.For<INbpGoldPriceClient>()`) | nothing changes |
  | implements it (`class MyGoldClient : INbpGoldPriceClient`) | build error CS0535 until the method is added (a stub is enough) |
  | uses an implementation compiled against the old version (e.g. a decorator from another package) | `TypeLoadException` when that type loads, until it is rebuilt with the method |

* Telemetry interfaces (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource`, `IOpenUrzednikSpan`) are frozen, because the library calls them: a new capability comes as a separate interface checked with `is` ([ADR-0003](0003-telemetry-abstractions.md)).

**Classes and models.**

* Public classes not designed for inheritance are `sealed`: the clients, `NbpOptions`, the adapters, the concrete errors and exceptions. The abstract bases (`OpenUrzednikError`, `OpenUrzednikException`, `ValueValidator<T>`) are the extension points. Removing `sealed` later is allowed in a minor version; adding it is breaking.
* Public models are `sealed record`s. New data comes as non-`required` `init` properties, never as a new positional parameter, which would change the constructor and `Deconstruct`; a `required` property would break every caller that creates the model (CS9035). A record that overrides `Equals`/`GetHashCode` (because it holds a list) includes the new property in both.

**Changing and removing API.**

* A member is marked `[Obsolete]` with the replacement in its message for at least one minor version before a major version removes it.
* The PR that makes a breaking change also raises `version.json` on `develop` to the next major ([ADR-0008](0008-branching-versioning-and-release.md)), and carries the `breaking-change` label.

### Consequences

* Good, because users know what a minor upgrade can change, including the parts SemVer leaves open (behaviour, errors, telemetry, targets).
* Good, because client interfaces can grow without a major version, and mocks keep working.
* Good, because sealed classes leave room to add members and change internals without breaking derived classes.
* Bad, because a user's own implementation of a client interface may need a stub after a minor upgrade, and one compiled against the old version (e.g. in another package) fails to load until it is rebuilt.
* Bad, because users can't derive from the clients; they decorate or mock the interfaces instead.
* Bad, because positional model constructors stay frozen, so newer properties are set differently from older ones.

### Confirmation

* Package validation compares the public API with the last release on every build and fails on breaking changes ([#94](https://github.com/milten89/OpenUrzednik.NET/issues/94); baseline 1.0.0 after the release, [#117](https://github.com/milten89/OpenUrzednik.NET/issues/117)). It also reports a method added to a client interface (CP0006); a minor version that adds one suppresses exactly that diagnostic, naming this ADR in the PR. Any other suppression outside a major version is a policy violation.
* The `reviewer` agent flags: an in-scope break in a PR that isn't for a major version; a new public class that isn't `sealed` and isn't designed for inheritance (already before 1.0, because sealing later is breaking); new public API in a `hotfix/*` PR; a positional or `required` property added to a model; an abstract member added to an extension point; a removal without a prior `[Obsolete]`.

## Pros and Cons of the Options

### An explicit scope, with client interfaces that may grow and sealed classes

* Good, because one rule covers API, behaviour, errors, telemetry and targets.
* Good, because adding a client method is a normal minor change.
* Bad, because implementers of client interfaces must add stubs.

### Frozen interfaces, with new methods as default interface members or as new interfaces

* Good, because a user's implementation never breaks.
* Bad, because default interface members don't exist on netstandard2.0 and .NET Framework ([ADR-0005](0005-target-frameworks.md)).
* Bad, because a new interface per addition (`INbpGoldPriceClient2`) fragments the API that most users only call or mock.

### No written policy: SemVer as commonly understood

* Good, because there is nothing to maintain.
* Bad, because behaviour, error codes, telemetry names and targets would be judged case by case, and users couldn't rely on them.

## More Information

* The support policy (which versions get fixes, security reporting) is published with SECURITY.md and the README's "Versioning and support" section ([#102](https://github.com/milten89/OpenUrzednik.NET/issues/102)).
* The .NET breaking-change rules: [Breaking changes in .NET libraries](https://learn.microsoft.com/dotnet/standard/library-guidance/breaking-changes).
