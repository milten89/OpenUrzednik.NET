---
status: accepted
date: 2026-10-01
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
| Integration | `OpenUrzednik.Nbp.DependencyInjection`, `OpenUrzednik.Extensions.Logging`, `OpenUrzednik.Diagnostics` | `Microsoft.Extensions.*` (DI, Options, Http, Http.Resilience, Logging) and the package they integrate (e.g. `System.Diagnostics.DiagnosticSource` on netstandard2.0, where `ActivitySource` isn't built in). |
| Tests | `tests/*` | Anything appropriate. |

* Build-only tooling (`Nerdbank.GitVersioning`, analyzers) uses `PrivateAssets="all"` and never becomes a package dependency.
* Versions are managed centrally in `Directory.Packages.props`; netstandard2.0-only references use a `Condition` on `TargetFramework`.
* DI packages expose `services.AddOpenUrzednik<Provider>(...)` registering typed `HttpClient`s and validated options, and return the `IHttpClientBuilder`.
* Resilience follows the .NET standard ([Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)): the app opts in by chaining `AddStandardResilienceHandler(...)` (or `AddResilienceHandler(...)`) on that builder. DI packages don't add a resilience handler themselves, because the guidance is one handler per client, not stacked.
* DI packages translate the exceptions a resilience handler throws when it rejects a request (`TimeoutRejectedException`, an open circuit breaker, the rate limiter) into errors, so they don't escape the result ([ADR-0002](0002-result-pattern-and-error-handling.md)). A timeout becomes a `RequestTimeoutError`; a rejection becomes a `ServiceUnavailableError`. Caller cancellation still throws.

### Consequences

* Good, because consumers not using Microsoft.Extensions pay nothing.
* Good, because conflicts with consumers' Microsoft.Extensions versions are limited to opt-in packages.
* Bad, because there are more packages to version, publish and document.
* Bad, because some convenience (e.g. `IOptions<T>` in client constructors) is not available in provider packages; clients take plain option objects.

### Confirmation

`dotnet list package --include-transitive` for each `src/` provider/Core project shows no non-BCL dependencies; the `reviewer` agent flags new `PackageReference`s in Core/provider projects.

## More Information

**2026-10-02 clarification.** The first version said DI packages register clients "with sensible resilience defaults". That would make the library add Polly's standard handler itself, and an app adding its own would then stack two handlers, which Microsoft's guidance advises against. The resilience is now the app's explicit choice, through the standard API, and the DI package only guarantees that the handler's rejections are returned as errors. The tracing adapter is named `OpenUrzednik.Diagnostics` ([ADR-0003](0003-telemetry-abstractions.md)).
