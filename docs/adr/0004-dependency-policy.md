---
status: accepted
date: 2026-10-02
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
| Shared infrastructure | `OpenUrzednik.Http` | Same as Core + `OpenUrzednik.Core`. |
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
  * Caller cancellation still throws. `OpenUrzednik.Http` itself doesn't reference Polly.

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
* A timeout rejection becomes a `TaskCanceledException` whose inner exception is the `TimeoutRejectedException`, not a `TimeoutException`. The request executor reports a limit in `RequestTimeoutError.Timeout` only when it knows that limit elapsed: its own deadline cancelled the request, or `HttpClient.Timeout` fired (an inner `TimeoutException`). A resilience handler's timeout is reported without a limit, instead of as `HttpClient.Timeout`.
* Options are checked at startup (`ValidateOnStart`) with the clients' own rules, exposed as `NbpOptions.Validate()`.

**2026-10-04 change: dependency versions.** The maintainer decided how to choose the version of a package the libraries depend on:

* **Abstractions** (API contracts under semantic versioning, e.g. `Microsoft.Extensions.Logging.Abstractions`): the lowest version: the first release (`x.0.0`) of the major that matches the oldest .NET target (currently 8.0.0). A later patch only if that version has a known vulnerability; the minimum rises when the oldest .NET target is removed ([ADR-0005](0005-target-frameworks.md)). A library's dependency version is a minimum for every app that uses it, so a higher one forces upgrades for no gain. `Microsoft.Extensions.Logging.Abstractions` moved from 10.0.10 to 8.0.0.
* **Implementations** (code that runs, e.g. `System.Text.Json`, `Microsoft.Extensions.Http`): the latest version, so the apps get the bug and security fixes.
* `Directory.Packages.props` groups the packages accordingly, and Dependabot has a separate `abstractions` group, so raising one comes as a PR of its own. That PR needs the maintainer's explicit answer first; agents stop and ask (CLAUDE.md).
* Accepted consequence: `OpenUrzednik.Nbp.DependencyInjection` still brings `Microsoft.Extensions.*` 10.x (Http, Options and, through them, the abstractions) into a .NET 8 app, because the implementations stay at the latest version.
* `Polly.Core` is an implementation: it is the resilience engine the app runs, even though the DI package only uses its exception types. It moves from 8.4.2 to the latest version (8.8.0), which replaces the earlier "version `Microsoft.Extensions.Http.Resilience` needs" choice above.
