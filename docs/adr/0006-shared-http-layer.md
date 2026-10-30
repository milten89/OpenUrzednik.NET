---
status: accepted
date: 2026-10-29
decision-makers: milten89
---

# 0006. Shared HTTP layer for REST providers

## Context and Problem Statement

`OpenUrzednik.Nbp` contains HTTP plumbing (`HttpClientExtensions.GetNbpAsync`): sending requests, mapping status codes to errors, reading `Retry-After`, deserializing JSON, recording telemetry. Future REST providers (MF Biała Lista, KRS) need exactly the same behaviour, and it must implement [ADR-0002](0002-result-pattern-and-error-handling.md) consistently (timeouts, network failures, disposal). GUS BIR uses SOAP. Where does shared HTTP code live?

## Decision Drivers

* One implementation of status mapping, timeout detection, disposal and telemetry.
* Keep `OpenUrzednik.Core` small, stable and free of HTTP/JSON concerns.
* Keep the public API that must stay stable after 1.0 as small as possible.

## Considered Options

* New package `OpenUrzednik.Http` for REST/JSON providers
* New package with pluggable deserialization (JSON and SOAP/XML)
* Shared source files compiled into each provider
* Put it into `OpenUrzednik.Core`

## Decision Outcome

Chosen option: "Shared source files compiled into each provider", because the layer is plumbing for provider authors, not API for applications, and a package would have to keep it stable under SemVer.

* The source lives in `src/Http` (`Infrastructure/`, `Polyfills/`), next to `src/Polyfills`. A REST provider imports `src/Http/Http.props`, which links the files into the provider, so each provider assembly has its own internal copy. The provider references `System.Net.Http.Json`, `System.Text.Json` and `Microsoft.Bcl.TimeProvider` on netstandard2.0 only ([ADR-0004](0004-dependency-policy.md)).
* The types are `internal`, in the namespace `OpenUrzednik.Http.Infrastructure`: the request executor (`RestRequestExecutor`) with send, status → error mapping (with provider-specific overrides, e.g. for JSON error bodies; the bounded plain-text read of a 400 body is part of the default mapping), timeout vs caller-cancellation detection, `Retry-After` parsing with `TimeProvider`, JSON deserialization via `JsonTypeInfo<T>` (source-generated, trimming/AOT friendly), disposal of request/response, telemetry spans and logs.
* No provider exposes these types in its public API. A provider's DI package links `Infrastructure/ResilienceRejection.cs` alone, to mark resilience-handler rejections for the executor ([ADR-0004](0004-dependency-policy.md)).
* `OpenUrzednik.Nbp` uses it first; NBP-specific behaviour stays in NBP. `tests/OpenUrzednik.Http.Tests` tests the layer through Nbp's copy (`InternalsVisibleTo`); only that copy is tested there, because a second provider granting the same access would give the test project two copies of each type.
* Shared transports are shared source. For SOAP (GUS) this is the default; the provider ADR that [ADR-0010](0010-provider-readiness-gate.md) requires may revisit it. If real duplication with a SOAP stack appears, a new ADR can extract a transport-only layer.

### Provider-author reference

* A provider describes itself with a `RestProviderProfile(name, displayName, mapErrorAsync)`. `name` builds the span name `<name>.http.get` and is the `{provider}` log property; `displayName` goes into error messages. Every log call of the layer has an event id from `RestLogEvents` (1xxx), which the provider's public log-events class mirrors ([ADR-0003](0003-telemetry-abstractions.md)). The provider passes its `ActivitySource` name to `OpenUrzednikTelemetry`, which uses it when the app gives no trace source (on the .NET targets); for that, the provider is added to Core's .NET-only `InternalsVisibleTo`.
* `RestRequestExecutor` resolves request paths against an absolute base address and never changes the `HttpClient`, so one `HttpClient` can serve several providers. The request deadline (the timeout passed to it, or `HttpClient.Timeout` when none is set) covers reading the body too.
* `GetAsync` **returns** as errors: every non-success status, an empty, malformed or undecodable JSON body, network failures (`HttpRequestException`, or an `IOException` while reading the body) and timeouts while sending or reading a success body (an `OperationCanceledException` while the caller's token isn't cancelled). When the error body can't be read before the deadline, `ReadMessageAsync()` returns `null` and the error is still returned.
* **Thrown**:
  * `OperationCanceledException` when the caller's token is cancelled;
  * `ArgumentException` and its subtypes for programmer errors, from the constructors and `GetAsync` (e.g. a null `HttpClient` or profile, an empty provider name, a relative base address, an invalid timeout, a null, empty or whitespace path, a null `JsonTypeInfo`);
  * any exception that `mapErrorAsync` throws, an `OperationCanceledException` included, and any other exception that comes out of the `HttpClient` pipeline. A resilience handler's rejections (Polly's `ExecutionRejectedException` subtypes) are such exceptions. They become errors only when a handler turns them into the exceptions above, as `OpenUrzednik.Nbp.DependencyInjection` does ([ADR-0004](0004-dependency-policy.md)).
* If an API reports errors in an unusual shape, pass `mapErrorAsync`. It gets an `ErrorResponseContext` with the status, the response (for headers), the request path, the `Retry-After` delay and a token. Read the body only through `ReadMessageAsync()`: it reads at most the first 500 characters as text, once, and the default mapping reuses the same read. When `mapErrorAsync` returns `null`, the default mapping applies. The context is valid only until its task completes.

### Consequences

* Good, because Core stays pure (results, errors, telemetry abstractions, validation).
* Good, because there is no extra package and no public API to keep stable; the layer can change in any version.
* Bad, because each provider assembly carries its own copy of the code.
* Bad, because a fix in the layer reaches users only through new provider versions (with lockstep versioning, [ADR-0008](0008-branching-versioning-and-release.md), every release carries it anyway).

### Confirmation

Provider packages contain no direct `HttpClient.SendAsync` calls; all HTTP goes through `RestRequestExecutor`. No public signature of a provider uses a type from `OpenUrzednik.Http.Infrastructure`. The `reviewer` agent checks both.

## Pros and Cons of the Options

### New package `OpenUrzednik.Http`

* Good, because one compiled copy serves every provider.
* Bad, because its types must be public to work across assemblies, so they become API that must stay stable for providers built on it.
* Bad, because one more package must be versioned, published and documented.

### Put it into `OpenUrzednik.Core`

* Good, because there is one package fewer.
* Bad, because Core gains HTTP/JSON code (and `System.Text.Json` on netstandard2.0) that SOAP providers do not need.

### Shared source files

* Good, because there is no extra public API or package.
* Bad, because code is duplicated across binaries, and fixes ship through each provider.

## More Information

**2026-10-02 note (backlog item 12).** Implemented: `RestRequestExecutor`, `RestProviderProfile` and `ErrorResponseContext` in `OpenUrzednik.Http.Infrastructure`, with `OpenUrzednikTelemetry` replacing `NbpTelemetryProvider`. The bounded read of a 400 body into `BadRequestError` turned out to be provider-neutral, so it is part of the default mapping and NBP needs no override; `RestProviderProfile.MapErrorAsync` is the hook for providers whose error bodies need parsing (e.g. JSON error codes). Log templates are provider-neutral and take the provider's display name as the `{provider}` property.

**2026-10-17 change: one version for all packages.** All packages now share one version from the root `version.json` ([ADR-0008](0008-branching-versioning-and-release.md)), so a provider no longer path-filters `OpenUrzednik.Http`, and an HTTP change no longer re-versions only the REST providers. The driver about needless version bumps, the matching consequence and the `pathFilters` check in Confirmation were removed. The version arguments under "Pros and Cons of the Options" no longer tell the options apart.

**2026-10-29 change: shared source instead of a package** ([#85](https://github.com/milten89/OpenUrzednik.NET/issues/85)). The maintainer chose shared source before 1.0. The `OpenUrzednik.Http` package would have frozen four public types meant only for provider authors, while no application needs them, and with one version for all packages (2026-10-17) a separate package no longer saved releases. The code moved from `src/OpenUrzednik.Http` to `src/Http`, the types became `internal`, and `OpenUrzednik.Nbp` compiles them in and gained the netstandard2.0 `System.Net.Http.Json` reference. The package's README was removed; its provider-author reference is now the section above, which also corrects its claim that only caller cancellation throws. The package is no longer published and gets deprecated on nuget.org ([#116](https://github.com/milten89/OpenUrzednik.NET/issues/116)). The Decision Outcome, drivers, consequences, Confirmation and the pros and cons were rewritten for the chosen option.
