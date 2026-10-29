---
status: accepted
date: 2026-10-29
decision-makers: milten89
---

# 0004. Dependency policy

## Context and Problem Statement

Libraries used by companies are judged by their dependency graph: every transitive package is a version conflict, a licence review and a potential vulnerability. At the same time consumers expect DI registration, `IHttpClientFactory` and resilience integration. Which dependencies may each package take?

## Decision Drivers

* Minimal, predictable dependency graph for Core and provider packages.
* netstandard2.0 support ([ADR-0005](0005-target-frameworks.md)) needs some out-of-band BCL packages.
* First-class experience for consumers using `Microsoft.Extensions.*`.

## Considered Options

* Microsoft.Extensions abstractions allowed everywhere; DI inside provider packages
* No dependencies in Core/providers; DI and integrations in separate packages
* Zero dependencies everywhere, no DI support

## Decision Outcome

Chosen option: "No dependencies in Core/providers; DI and integrations in separate packages".

| Package kind | Examples | Allowed dependencies |
|---|---|---|
| Core | `OpenUrzednik.Core` | None on .NET targets. On netstandard2.0 only official Microsoft out-of-band BCL packages (e.g. `System.Text.Json`, `Microsoft.Bcl.TimeProvider`, `System.Memory`). |
| Shared source (not a package) | `src/Http` ([ADR-0006](0006-shared-http-layer.md)) | None of its own: it is compiled into each REST provider, which references what it needs on netstandard2.0 (`System.Net.Http.Json`, `System.Text.Json`, `Microsoft.Bcl.TimeProvider`). |
| Provider | `OpenUrzednik.Nbp` | Same as Core + other OpenUrzednik packages. |
| Integration | `OpenUrzednik.Nbp.DependencyInjection`, `OpenUrzednik.Extensions.Logging` | `Microsoft.Extensions.*` (DI, Options, Http, Http.Resilience, Logging) and the package they integrate. DI packages may also use `Polly.Core`, which `Microsoft.Extensions.Http.Resilience` brings in, to recognise rejections (below). |
| Tracing adapter | `OpenUrzednik.Diagnostics` | `System.Diagnostics.DiagnosticSource` on netstandard2.0 only (`ActivitySource` is built into .NET). |
| Tests | `tests/*` | Anything appropriate. |

* Build-only tooling (`Nerdbank.GitVersioning`, analyzers) uses `PrivateAssets="all"` and never becomes a package dependency.
* Versions are managed centrally in `Directory.Packages.props`; netstandard2.0-only references use a `Condition` on `TargetFramework`.
* DI packages expose `services.AddOpenUrzednik<Provider>(...)` registering typed `HttpClient`s and validated options, and return the `IHttpClientBuilder`.
* Resilience follows the .NET standard ([Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)): the app opts in by chaining `AddStandardResilienceHandler(...)` (or `AddResilienceHandler(...)`) on that builder. DI packages don't add a resilience handler themselves, because the guidance is one handler per client, not stacked.
* DI packages translate the exceptions a resilience handler throws when it rejects a request, so they don't escape the result ([ADR-0002](0002-result-pattern-and-error-handling.md)). They register an outermost `DelegatingHandler` (before any resilience handler) that catches Polly's `ExecutionRejectedException` types and rethrows them as exceptions the request executor ([ADR-0006](0006-shared-http-layer.md)) already handles:
  * `TimeoutRejectedException` → `TaskCanceledException` (inner: the rejection), returned as a `RequestTimeoutError`;
  * other rejections (open circuit breaker, rate limiter) → `HttpRequestException`, returned as a `ServiceUnavailableError`.
  * Caller cancellation still throws. The shared HTTP layer itself doesn't reference Polly.

### Consequences

* Good, because consumers not using Microsoft.Extensions pay nothing.
* Good, because conflicts with consumers' Microsoft.Extensions versions are limited to opt-in packages.
* Bad, because there are more packages to version, publish and document.
* Bad, because some convenience (e.g. `IOptions<T>` in client constructors) is not available in provider packages; clients take plain option objects.

### Confirmation

`dotnet list package --include-transitive` for each `src/` provider/Core project shows no non-BCL dependencies; the `reviewer` agent flags new `PackageReference`s in Core/provider projects.

## More Information

**2026-10-02 change.** The first version said DI packages register clients "with sensible resilience defaults". That would make the library add Polly's standard handler itself, and an app adding its own would then stack two handlers, which Microsoft's guidance advises against. The resilience is now the app's explicit choice, through the standard API, and the DI package only guarantees that the handler's rejections are returned as errors. The tracing adapter is named `OpenUrzednik.Diagnostics` ([ADR-0003](0003-telemetry-abstractions.md)).

**2026-10-02 implementation note** (`OpenUrzednik.Nbp.DependencyInjection`, backlog item 15):

* The DI package references `Polly.Core` (8.4.2, the version `Microsoft.Extensions.Http.Resilience` 10.8 needs) only to recognise the rejection types, not `Microsoft.Extensions.Http.Resilience` itself: apps that don't add a resilience handler don't get it.
* A timeout rejection becomes a `TaskCanceledException` whose inner exception is the `TimeoutRejectedException`, not a `TimeoutException`. The request executor reports a limit in `RequestTimeoutError.Timeout` only when it knows that limit elapsed: its own deadline cancelled the request, or `HttpClient.Timeout` fired. A resilience handler's timeout is reported without a limit, instead of as `HttpClient.Timeout`. (2026-10-02, backlog item 17: "`HttpClient.Timeout` fired" was first detected by an inner `TimeoutException`, which .NET Framework doesn't set; the executor now checks that at least `HttpClient.Timeout` has elapsed.)
* Options are checked at startup (`ValidateOnStart`) with the clients' own rules, exposed as `NbpOptions.Validate()`.

**2026-10-04 change: dependency versions.** The maintainer decided how to choose the version of a package the published libraries depend on. The rule covers only those dependencies, which become minimums for the apps; tests, build tooling and anything used only inside the repository take the latest version (clarified 2026-10-04).

* **Abstractions** (API contracts under semantic versioning, e.g. `Microsoft.Extensions.Logging.Abstractions`): the lowest version: the first release (`x.0.0`) of the major that matches the oldest .NET target (currently 8.0.0). A later patch only if that version has a known vulnerability; the minimum rises when the oldest .NET target is removed ([ADR-0005](0005-target-frameworks.md)). A library's dependency version is a minimum for every app that uses it, so a higher one forces upgrades for no gain. `Microsoft.Extensions.Logging.Abstractions` moved from 10.0.10 to 8.0.0.
* **Implementations** (code that runs, e.g. `System.Text.Json`, `Microsoft.Extensions.Http`): the latest version, so the apps get the bug and security fixes. (Narrowed on 2026-10-29 to the latest patch of the chosen minor, see below.)
* `Directory.Packages.props` groups the packages accordingly, and Dependabot ignores version updates of `*.Abstractions` (see the 2026-10-04 note). Any change that raises an abstraction needs the maintainer's explicit answer first; agents stop and ask (CLAUDE.md).
* Accepted consequence: `OpenUrzednik.Nbp.DependencyInjection` still brings `Microsoft.Extensions.*` 10.x (Http, Options and, through them, the abstractions) into a .NET 8 app, because the implementations stay at the latest version. (Reversed on 2026-10-17, see the note below.)
* `Polly.Core` is an implementation: it is the resilience engine the app runs, even though the DI package only uses its exception types. It moves from 8.4.2 to the latest version (8.8.0), which replaces the earlier "version `Microsoft.Extensions.Http.Resilience` needs" choice above.

**2026-10-04 change.** Dependabot opened the `Microsoft.Extensions.Logging.Abstractions` 8.0.0 → 10.x raise twice (#41, #56); both were closed under this rule. `.github/dependabot.yml` now ignores every version update of `*.Abstractions` instead of grouping them. A vulnerability still shows as a Dependabot alert, which doesn't depend on that file; the version is then raised by hand to the first fixed patch. The minimum is also raised by hand when the oldest .NET target is removed (ADR-0005).

**2026-10-17 note: `System.Diagnostics.DiagnosticSource`** ([#77](https://github.com/milten89/OpenUrzednik.NET/issues/77)). Only the tracing adapter `OpenUrzednik.Diagnostics` references it, and only on netstandard2.0; Core, Http and the provider packages don't ([ADR-0003](0003-telemetry-abstractions.md)). `CLAUDE.md` had listed it among the netstandard2.0 packages allowed in Core and providers; that line, and the package list in ADR-0005, are corrected. It is classified as an implementation and takes the latest version, although [Microsoft's library guidance](https://learn.microsoft.com/dotnet/standard/library-guidance/dependencies) suggests the oldest supported version for netstandard libraries: on .NET Framework the package carries the `Activity` and `ActivitySource` code itself, so its fixes reach the app only through a newer version.

**2026-10-17 change: per-target versions for the DI package** ([#84](https://github.com/milten89/OpenUrzednik.NET/issues/84)). The accepted consequence above is reversed. `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options` were one version for every target, the latest 10.0.x, so a .NET 8 app using `OpenUrzednik.Nbp.DependencyInjection` was moved to `Microsoft.Extensions.*` 10.x, a major ahead of its own framework. Each target now gets the latest patch of its own major: `net8.0` → 8.0.x, `net10.0` → 10.0.x, and `netstandard2.0` → 10.0.x, the LTS line that still supports .NET Framework (conditional `PackageVersion` rows in `Directory.Packages.props`). They are still implementations, so each row takes its line's latest patch.

* Dependabot no longer proposes major updates of the published libraries' dependencies (`Microsoft.Extensions.Http`, `Microsoft.Extensions.Options`, `System.Text.Json`, `System.Net.Http.Json`, `System.Diagnostics.DiagnosticSource`, `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode`, `Polly.Core`): their major follows our targets, and a new major raises the minimum for every app. Minor and patch updates still come (narrowed on 2026-10-29 to patches only, see below); test and tooling packages keep getting majors. A new major arrives together with a new target (ADR-0005).
* Dependabot reads `Directory.Packages.props` without a target, so the two rows of one package may confuse it: a `net8.0` row it doesn't patch is patched by hand, and a Dependabot PR that moves a `net8.0` row to 10.x is corrected by hand before merging. The 8.0.x line gets no patches after .NET 8 reaches end of support on 2026-11-10; the `net8.0` rows then stay as they are and go away when `net8.0` is removed in a major version (ADR-0005).

**2026-10-29 change: patch-only updates and one override row** ([#148](https://github.com/milten89/OpenUrzednik.NET/issues/148)). Dependabot skipped every package that had two conditional `PackageVersion` rows: in #140 and #145 it moved the other implementations to 10.0.14 but left `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options` at 10.0.12. Each now has one normal row (10.0.x, used by `net10.0` and `netstandard2.0`) and a `net8.0`-only `PackageVersion Update` override (8.0.x) in its own item group, which Dependabot is expected to leave alone (to be confirmed on its first run after this change); the override is patched by hand until 8.0.x stops getting patches on 2026-11-10. The maintainer also narrowed "implementations take the latest version" to **the latest patch of the chosen minor**: each version is a minimum for every app, so a minor raise (e.g. `Polly.Core` 8.8 → 8.9) is now a deliberate change rather than a Dependabot PR. Dependabot proposes patches only for the published libraries' dependencies, as one grouped PR; a vulnerability fixed only in a newer minor still shows as a Dependabot alert and is raised by hand.

**2026-10-29 note: `OpenUrzednik.Http` is shared source** ([#85](https://github.com/milten89/OpenUrzednik.NET/issues/85)). The HTTP layer is no longer a package but internal source compiled into each REST provider ([ADR-0006](0006-shared-http-layer.md)). The "Shared infrastructure" row of the table became "Shared source", and `OpenUrzednik.Nbp` now references `System.Net.Http.Json` on netstandard2.0 itself.
