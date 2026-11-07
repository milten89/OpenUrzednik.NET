---
status: accepted
date: 2026-11-07
confidence: high
decision-makers: milten89
---

# 0019. Shared HTTP layer for REST providers, tested through a test-only host

## Context and Problem Statement

Every REST provider (NBP, and the planned MF Biała Lista and KRS) needs the same HTTP plumbing: sending requests, mapping status codes to errors, reading `Retry-After`, deserializing JSON, recording telemetry. It must implement [ADR-0002](0002-result-pattern-and-error-handling.md) consistently (timeouts, network failures, disposal). GUS BIR uses SOAP. Where does shared HTTP code live, and where is it tested?

This record supersedes [ADR-0006](0006-shared-http-layer.md). It keeps its decision and changes where the layer is tested: through a test-only library instead of the NBP provider's copy, so that mutation testing ([ADR-0018](0018-mutation-testing-before-release.md)) mutates the layer once, by its own tests, instead of in every REST provider's shard.

## Decision Drivers

* One implementation of status mapping, timeout detection, disposal and telemetry.
* Keep `OpenUrzednik.Core` small, stable and free of HTTP/JSON concerns.
* Keep the public API that must stay stable after 1.0 as small as possible.
* Test the layer once, by its own tests, with no provider's code in the way; mutate it once ([ADR-0018](0018-mutation-testing-before-release.md)).

## Considered Options

* New package `OpenUrzednik.Http` for REST/JSON providers
* New package with pluggable deserialization (JSON and SOAP/XML)
* Shared source files compiled into each provider
* Put it into `OpenUrzednik.Core`

Where the layer's tests reach it:

* Through the first provider's copy (`OpenUrzednik.Nbp`, `InternalsVisibleTo`)
* Through a test-only library that compiles the layer alone
* Compiled into the test project itself

## Decision Outcome

Chosen option: "Shared source files compiled into each provider", because the layer is plumbing for provider authors, not API for applications, and a package would have to keep it stable under SemVer. The tests reach it "Through a test-only library that compiles the layer alone", because it tests the layer without any provider and Stryker.NET can mutate it there once.

* The source lives in `src/Http` (`Infrastructure/`, `Polyfills/`), next to `src/Polyfills`. A REST provider imports `src/Http/Http.props`, which links the files into the provider, so each provider assembly has its own internal copy. The provider references `System.Net.Http.Json`, `System.Text.Json` and `Microsoft.Bcl.TimeProvider` on netstandard2.0 only ([ADR-0004](0004-dependency-policy.md)).
* The types are `internal`, in the namespace `OpenUrzednik.Http.Infrastructure`: the request executor (`RestRequestExecutor`) with send, status → error mapping (with provider-specific overrides, e.g. for JSON error bodies; the bounded plain-text read of a 400 body is part of the default mapping), timeout vs caller-cancellation detection, `Retry-After` parsing with `TimeProvider`, JSON deserialization via `JsonTypeInfo<T>` (source-generated, trimming/AOT friendly), disposal of request/response, telemetry spans and logs.
* No provider exposes these types in its public API. A provider's DI package links `Infrastructure/ResilienceRejection.cs` alone, to mark resilience-handler rejections for the executor ([ADR-0004](0004-dependency-policy.md)). A DI package of a provider with display paths also imports `src/Http/DependencyInjection/DependencyInjection.props`, its HttpClient logging ([ADR-0017](0017-http-client-logging-for-identifier-paths.md)).
* `OpenUrzednik.Nbp` is the first REST provider on the layer; NBP-specific behaviour stays in NBP.
* `tests/OpenUrzednik.Http.TestHost`, a test-only library (not packed), imports `Http.props` and references only `OpenUrzednik.Core`; `tests/OpenUrzednik.Http.Tests` tests the layer through its copy (`InternalsVisibleTo`), and no provider grants the test project access to its copy, so the test project sees one copy of each type. On the .NET targets `OpenUrzednikTelemetry` calls Core's internal `SharedActivityTraceSource`, so Core grants the host test-only `InternalsVisibleTo` there, next to the REST providers. The layer is mutated once, in the host's shard; a provider's shard excludes it (`!**/Http/**`, [ADR-0018](0018-mutation-testing-before-release.md)).
* Shared transports are shared source. For SOAP (GUS) this is the default; the provider ADR that [ADR-0016](0016-next-providers-and-package-naming.md) requires may revisit it. If real duplication with a SOAP stack appears, a new ADR can extract a transport-only layer.

### Provider-author reference

* A provider describes itself with a `RestProviderProfile(name, displayName, mapErrorAsync)`. `name` builds the span name `<name>.http.get` and is the `{provider}` log property; `displayName` goes into error messages. The layer's log templates are provider-neutral. Every log call of the layer has an event id from `RestLogEvents` (1xxx), which the provider's public log-events class mirrors ([ADR-0003](0003-telemetry-abstractions.md)). The provider passes its `ActivitySource` name to `OpenUrzednikTelemetry`, which uses it when the app gives no trace source (on the .NET targets); for that, the provider is added to Core's .NET-only `InternalsVisibleTo`.
* A REST provider whose request paths or queries carry personal identifiers ([ADR-0003](0003-telemetry-abstractions.md)) must give the executor a display path or route template (e.g. `/api/search/nip/{nip}`). The executor uses the absolute `RequestUri` in every status-error message, in the error log and in `ErrorResponseContext.RequestPath` (passed to `MapErrorAsync`), so the display value must replace the whole request URI (path and query) in `url.path`, logs, error messages and `ErrorResponseContext.RequestPath`. The executor takes it through `GetAsync(relativePath, displayPath, typeInfo, …)`; the default 400 mapping then leaves out the server's message, which may echo the identifier, and a provider that needs it reads and masks it in `mapErrorAsync`.
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
* Good, because the layer's tests and mutants run once, without a provider, and each provider's shard pays only for its own code.
* Bad, because Core grants one more test-only `InternalsVisibleTo` on the .NET targets, and the host repeats on net472 what a provider references for netstandard2.0.

### Confirmation

REST/JSON provider packages contain no direct `HttpClient.SendAsync` calls; all their HTTP goes through `RestRequestExecutor`. A SOAP or other transport follows its provider ADR ([ADR-0016](0016-next-providers-and-package-naming.md)). No public signature of a provider uses a type from `OpenUrzednik.Http.Infrastructure`. No test project references a provider to reach the layer, and the `http` mutation shard (`tests/Mutation/shards.json`) mutates `src/Http/Infrastructure` while provider shards exclude it. The `reviewer` agent checks these.

## Pros and Cons of the Options

### New package `OpenUrzednik.Http`

* Good, because one compiled copy serves every provider.
* Bad, because its types must be public to work across assemblies, so they become API that must stay stable for providers built on it: four public types meant only for provider authors, which no application needs.
* Bad, because one more package must be versioned, published and documented, and with one version for all packages ([ADR-0008](0008-branching-versioning-and-release.md)) a separate package saves no releases.

### New package with pluggable deserialization (JSON and SOAP/XML)

* Good, because the SOAP provider (GUS BIR) could use the same layer.
* Bad, because it is a public package too, with the costs of `OpenUrzednik.Http` above.
* Bad, because no SOAP stack exists yet to show what it would share; if real duplication appears, a new ADR can extract a transport-only layer.

### Put it into `OpenUrzednik.Core`

* Good, because there is one package fewer.
* Bad, because Core gains HTTP/JSON code (and `System.Text.Json` on netstandard2.0) that SOAP providers do not need.

### Shared source files

* Good, because there is no extra public API or package.
* Bad, because code is duplicated across binaries, and fixes ship through each provider.

### Tests through the first provider's copy

* Good, because no extra project or `InternalsVisibleTo` in Core is needed.
* Bad, because the provider's mutation shard mutates the layer too, and every later provider would mutate it again unless it excludes it; locally the NBP shard took 273 s, and the layer alone with its own tests 155 s.
* Bad, because the layer's tests depend on a provider package.

### Test-only library that compiles the layer alone

* Good, because the layer is tested and mutated once, without a provider ([ADR-0018](0018-mutation-testing-before-release.md) uses the same kind of host for `src/Identifiers` and `src/Http/DependencyInjection`).
* Bad, because Core grants it test-only `InternalsVisibleTo` on the .NET targets.

### Compiled into the test project

* Good, because no extra project is needed.
* Bad, because Stryker.NET never mutates files compiled into a test project.

## More Information

* Supersedes [ADR-0006](0006-shared-http-layer.md), which tested the layer through Nbp's copy ([#236](https://github.com/milten89/OpenUrzednik.NET/issues/236)).
* The changes before 1.0 are listed in [ADR-0006](0006-shared-http-layer.md#changes-before-10).
* Follow-up: deprecating the no longer published `OpenUrzednik.Http` package on nuget.org ([#116](https://github.com/milten89/OpenUrzednik.NET/issues/116)).
