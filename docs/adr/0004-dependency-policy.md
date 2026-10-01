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
| Integration | `OpenUrzednik.Nbp.DependencyInjection`, `OpenUrzednik.Extensions.Logging`, `OpenUrzednik.OpenTelemetry` | `Microsoft.Extensions.*` (DI, Options, Http, Http.Resilience, Logging) and the package they integrate. |
| Tests | `tests/*` | Anything appropriate. |

* Build-only tooling (`Nerdbank.GitVersioning`, analyzers) uses `PrivateAssets="all"` and never becomes a package dependency.
* Versions are managed centrally in `Directory.Packages.props`; netstandard2.0-only references use a `Condition` on `TargetFramework`.
* DI packages expose `services.AddOpenUrzednik<Provider>(...)` registering typed `HttpClient`s with sensible resilience defaults and validated options.

### Consequences

* Good, because consumers not using Microsoft.Extensions pay nothing.
* Good, because conflicts with consumers' Microsoft.Extensions versions are limited to opt-in packages.
* Bad, because there are more packages to version, publish and document.
* Bad, because some convenience (e.g. `IOptions<T>` in client constructors) is not available in provider packages; clients take plain option objects.

### Confirmation

`dotnet list package --include-transitive` for each `src/` provider/Core project shows no non-BCL dependencies; the `reviewer` agent flags new `PackageReference`s in Core/provider projects.
