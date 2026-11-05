---
status: accepted
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0003. Telemetry abstractions and adapter packages

## Context and Problem Statement

Clients log and trace their work. The common .NET choice is `Microsoft.Extensions.Logging.ILogger` and `System.Diagnostics.ActivitySource`, but `ILogger` is a NuGet dependency, and on netstandard2.0 so is `ActivitySource` (`System.Diagnostics.DiagnosticSource`). The alternative is the library's own abstractions in Core (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource`, `IOpenUrzednikSpan`). Which should the clients use?

## Decision Drivers

* Core and provider packages depend on no third-party packages; on netstandard2.0 they use only the Microsoft BCL packages [ADR-0004](0004-dependency-policy.md) allows. `System.Diagnostics.DiagnosticSource` and `Microsoft.Extensions.Logging.Abstractions` stay excluded there, even though they are official Microsoft packages; `System.Diagnostics.DiagnosticSource` would add a package to every .NET Framework app for a feature only some use.
* Consumers using `ILogger`/OpenTelemetry must not have to write adapters themselves.
* Zero overhead when telemetry is not configured.

## Considered Options

* Keep custom abstractions and ship official adapter packages
* Use `ILogger` + `ActivitySource` directly
* Hybrid: `ActivitySource` directly, custom logger

## Decision Outcome

Chosen option: "Keep custom abstractions and ship official adapter packages".

* Core owns `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource`, `IOpenUrzednikSpan` plus `Null*` implementations used when nothing is configured (for tracing on the .NET targets, see the fallback source below).
* Official adapters are separate packages, for example:
  * `OpenUrzednik.Extensions.Logging`: `IOpenUrzednikLogger` over `ILogger` (`OpenUrzednikLogger`). The log state lists the named values followed by `{OriginalFormat}`, as `LoggerMessage` does, so structured providers keep the properties, and the event id becomes the `EventId`. `loggerFactory.CreateOpenUrzednikLogger<TClient>()` uses the client's full type name as the category (as `ILogger<T>` does), so `"OpenUrzednik.Nbp"` filters a whole provider.
  * `OpenUrzednik.Diagnostics`: `IOpenUrzednikTraceSource` over `ActivitySource` (`ActivityTraceSource`). Source names are `OpenUrzednik.<Provider>` (e.g. `OpenUrzednik.Nbp`), published by each provider as a constant (`NbpTelemetry.SourceName`), so OpenTelemetry users subscribe with `AddSource("OpenUrzednik.*")`. `ActivityTraceSource.GetShared(name)` keeps one source per name for the process. With no listener, or when the span isn't sampled, it returns the no-op span. It depends only on `System.Diagnostics.DiagnosticSource`, and only on netstandard2.0; no dependency on the OpenTelemetry packages. The package is named `OpenUrzednik.Diagnostics`, not `OpenUrzednik.OpenTelemetry`, because `ActivitySource` works with any listener, not only OpenTelemetry.
* Every span is `ActivityKind.Internal`, including `<provider>.http.get`: `HttpClient`'s own instrumentation creates the `Client` span under it, so the abstraction needs no `ActivityKind`. `RecordException` adds an `exception` event with the semantic-convention tags.
* On the .NET targets, where `ActivitySource` is built in, a client without a trace source uses its provider's shared source, so `AddSource("OpenUrzednik.*")` is enough without DI on .NET 8 and later. The implementation (`ActivitySpan`, `SharedActivityTraceSource`) is internal to Core and compiled only there; it is the only place Core uses `ActivitySource`. `OpenUrzednik.Diagnostics` and the REST providers reuse it through `InternalsVisibleTo`, which Core grants only on the .NET targets, because on netstandard2.0 the assemblies' internal polyfills would clash (each REST provider is added to it, [ADR-0006](0006-shared-http-layer.md)). `OpenUrzednik.Diagnostics` compiles the same files for netstandard2.0, where clients keep the no-op default.
* DI packages ([ADR-0004](0004-dependency-policy.md)) wire the adapters automatically when the consumer registers logging/tracing.

**Conventions:**

* Span names: `<provider>.<area>.<operation>` in snake_case, e.g. `nbp.currency.buy_sell_latest`, `nbp.http.get`.
* Tag names: `<provider>.<parameter>` (e.g. `nbp.currency`, `nbp.top_count`). Standard HTTP tags follow OpenTelemetry's HTTP client semantic conventions (e.g. `http.request.method`, `server.address`, `server.port`, `url.path`, `http.response.status_code`); the documented tags are listed in the [Diagnostics README](../../src/OpenUrzednik.Diagnostics/README.md) and the [NBP README](../../src/OpenUrzednik.Nbp/README.md#telemetry-reference).
* The HTTP span's `url.path` (the absolute path, e.g. `/api/cenyzlota`), `server.address` and `server.port` come from the address the client was given (`NbpOptions.ApiUrl`, the `HttpClient`'s base address or the default), before any `DelegatingHandler` rewrites it. The host is `Uri.IdnHost` (an IPv6 address without brackets, an international domain in ASCII), and `server.port` is always set, as .NET 9 and later do for their own HTTP spans. `url.full` is not set, because a provider's query string may carry personal identifiers. The host stays out of the logs, whose properties aren't part of the stable contract.
* Errors are recorded via `RecordError`/`RecordErrors` extensions, which set OpenTelemetry's `error.type` tag to `OpenUrzednikError.Code` (`RecordErrors`: the first error's code). The `error` events `RecordErrors` adds are best-effort. The `<provider>.http.get` span sets `error.type` and is marked as failed for every failure, a 404 included.
* Every log entry has an `OpenUrzednikEventId` (`Id`, `Name`), which `IOpenUrzednikLogger.Log` takes after the level: the shared HTTP layer's events are 1xxx, a provider's own 2xxx, and each provider lists all of them as constants (`NbpLogEvents`). The ids are fixed by the library, the same in every client of a provider, and unique within a log category. There is no hook to renumber them: apps that need their own numbering remap downstream (Serilog, OpenTelemetry).
* The `{provider}` log property is the provider's short name (`nbp`), as in span names; error messages keep the display name.
* Log calls at `Debug`/`Trace` are guarded with `IsEnabled`; messages use templates with named placeholders, never string interpolation.
* Never put secrets (API keys, session ids, tokens) or personal identifiers (PESEL, NIP, REGON, bank account numbers, whoever they belong to) in log messages or properties, span names or tags, error and exception messages, error metadata or `Exception.Data`. Show at most the last 4 characters (e.g. `*******1234`), or use a template or display value instead. A provider ADR may require stricter masking. The shared executor writes request paths into `url.path`, logs and the messages of the errors it returns, so an identifier in a path would leak through every channel: a provider whose paths carry identifiers also needs a display path ([ADR-0006](0006-shared-http-layer.md)).

**Compatibility of the telemetry contract** (from 1.0):

* **Stable within a major version:** source names, span names, the documented tags and their meaning, `error.type` and its values, logger categories, and event ids with their names (never renumbered or reused). Not covered: log message text and properties, the `error` and `exception` events, span status descriptions.
* **Evolution:** `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` don't change after 1.0, because the library calls them and other packages implement them. A new capability comes as a separate interface that the library checks with `is`, falling back when an implementation lacks it.
* **Cross-package internals:** on .NET, Diagnostics and the providers call Core's internal `SharedActivityTraceSource` and `ActivitySpan` through `InternalsVisibleTo`. Package dependencies are minimums ([ADR-0008](0008-branching-versioning-and-release.md)), so an app may pair a newer Core with older packages: within a major, those members are only added to, never changed or removed.
* **Sources are unversioned:** `ActivitySource` gets no version; the stability rule above takes its place.
* **Metrics are out of scope.** The library records no metrics; `HttpClient`'s own metrics cover requests. Adding metrics later needs a new decision.

### Consequences

* Good, because packages stay dependency-free and have the same API on netstandard2.0 and .NET; only the .NET default trace source differs.
* Good, because consumers of `ILogger`/OpenTelemetry get first-party adapters.
* Bad, because the abstractions must be kept expressive enough (structured properties, events, exceptions) and versioned carefully.
* Bad, because two more packages must be maintained.

### Confirmation

Adapter packages have tests asserting that spans/log entries produced by a client reach `ILogger`/`ActivityListener` (`NbpClientLoggingTest`, `NbpClientTracingTest`, including the .NET fallback source); `NbpLogEventsTest` checks that `NbpLogEvents` matches the events the clients log.

## Pros and Cons of the Options

### Use `ILogger` + `ActivitySource` directly

* Good, because it is the most common pattern in .NET libraries.
* Bad, because on .NET Framework (through netstandard2.0, [ADR-0005](0005-target-frameworks.md)) both are extra packages (`Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`) in the core and provider packages. The adapter packages bring the standard behaviour to apps that want it, and DI wires them automatically.

## More Information

### Changes before 1.0

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0003-telemetry-abstractions.md#more-information).

* 2026-10-02 · backlog item 16 · Both adapter packages were implemented, and the HTTP tags took OpenTelemetry's names (`url.path`, `http.response.status_code`, `http.request.method`).
* 2026-10-02 · Using `ILogger` and `ActivitySource` directly was reconsidered and rejected; the tracing adapter was named `OpenUrzednik.Diagnostics`.
* 2026-10-30 · [#91](https://github.com/milten89/OpenUrzednik.NET/issues/91) · The 1.0 telemetry contract: event ids, `error.type`, the short `{provider}` name, the .NET fallback source and the stability rules.
* 2026-10-30 · [#109](https://github.com/milten89/OpenUrzednik.NET/issues/109) · The rule against secrets and personal identifiers covers every telemetry and error channel and every holder of an identifier.
* 2026-11-05 · [#121](https://github.com/milten89/OpenUrzednik.NET/issues/121) · The HTTP span carries `server.address` and `server.port`.
