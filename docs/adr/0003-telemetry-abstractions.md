---
status: accepted
date: 2026-10-02
decision-makers: milten89
---

# 0003. Telemetry abstractions and adapter packages

## Context and Problem Statement

Clients log and trace their work. The common .NET choice is `Microsoft.Extensions.Logging.ILogger` and `System.Diagnostics.ActivitySource`, but `ILogger` is a NuGet dependency, and on netstandard2.0 so is `ActivitySource` (`System.Diagnostics.DiagnosticSource`). The library already defines its own `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` in Core. Should we keep them or switch to the standard types?

## Decision Drivers

* Core and provider packages depend on no third-party packages; on netstandard2.0 they use only Microsoft BCL packages ([ADR-0004](0004-dependency-policy.md)). `System.Diagnostics.DiagnosticSource` stays excluded there, because it would add a package to every .NET Framework app for a feature only some use.
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
  * `OpenUrzednik.Extensions.Logging` – `IOpenUrzednikLogger` over `ILogger`.
  * `OpenUrzednik.Diagnostics` – `IOpenUrzednikTraceSource` over `ActivitySource`. Source names are `OpenUrzednik.<Provider>` (e.g. `OpenUrzednik.Nbp`), so OpenTelemetry users subscribe with `AddSource("OpenUrzednik.*")`. It depends only on `System.Diagnostics.DiagnosticSource`, and only on netstandard2.0; no dependency on the OpenTelemetry packages.
* On the .NET targets, where `ActivitySource` is built in, a client without a trace source uses its provider's shared source. The implementation (`ActivitySpan`, `SharedActivityTraceSource`) is internal to Core and compiled only there; `OpenUrzednik.Diagnostics` reuses it through `InternalsVisibleTo` and compiles the same files for netstandard2.0, where clients keep the no-op default.
* DI packages ([ADR-0004](0004-dependency-policy.md)) wire the adapters automatically when the consumer registers logging/tracing.

**Conventions:**

* Span names: `<provider>.<area>.<operation>` in snake_case, e.g. `nbp.currency.buy_sell_latest`, `nbp.http.get`.
* Tag names: `<provider>.<parameter>` (e.g. `nbp.currency`, `nbp.top_count`); standard HTTP tags follow OpenTelemetry semantic conventions (`http.request.method`, `url.path`, `http.response.status_code`).
* Errors are recorded via `RecordError`/`RecordErrors` extensions, which set OpenTelemetry's `error.type` tag to `OpenUrzednikError.Code`. The `error` events `RecordErrors` adds are best-effort.
* Every log entry has an `OpenUrzednikEventId`: the shared HTTP layer's events are 1xxx, a provider's own 2xxx, and each provider lists all of them as constants (`NbpLogEvents`). The `{provider}` log property is the provider's short name (`nbp`), as in span names.
* Log calls at `Debug`/`Trace` are guarded with `IsEnabled`; messages use templates with named placeholders, never string interpolation.
* Never log secrets (API keys, session ids) or full personal identifiers (PESEL, NIP of natural persons) – mask them.

### Consequences

* Good, because packages stay dependency-free and have the same API on netstandard2.0 and .NET; only the .NET default trace source differs.
* Good, because consumers of `ILogger`/OpenTelemetry get first-party adapters.
* Bad, because the abstractions must be kept expressive enough (structured properties, events, exceptions) and versioned carefully.
* Bad, because two more packages must be maintained.

### Confirmation

Adapter packages have tests asserting that spans/log entries produced by a client reach `ILogger`/`ActivityListener` (`NbpClientLoggingTest`, `NbpClientTracingTest`, including the .NET fallback source); `NbpLogEventsTest` checks that `NbpLogEvents` matches the events the clients log.

## More Information

**2026-10-02 implementation note.** Both adapters exist (backlog item 16):

* `OpenUrzednik.Extensions.Logging`: `OpenUrzednikLogger` wraps an `ILogger`. The state lists the named values followed by `{OriginalFormat}`, as `LoggerMessage` does, so structured providers keep the properties. `loggerFactory.CreateOpenUrzednikLogger<TClient>()` uses the client's full type name as the category (as `ILogger<T>` does), so `"OpenUrzednik.Nbp"` filters a whole provider.
* `OpenUrzednik.Diagnostics`: `ActivityTraceSource` wraps an `ActivitySource`; `ActivityTraceSource.GetShared(name)` keeps one source per name for the process. Providers publish their source name as a constant (`NbpTelemetry.SourceName`). With no listener, or when the span isn't sampled, it returns the no-op span. Every span is `ActivityKind.Internal`, including `<provider>.http.get`: `HttpClient`'s own instrumentation creates the `Client` span under it, so the abstraction needs no `ActivityKind`. `RecordException` adds an `exception` event with the semantic-convention tags.
* The HTTP tags were renamed at the same time: `http.path` → `url.path` (now the absolute path, e.g. `/api/cenyzlota`), `http.status_code` → `http.response.status_code`, plus `http.request.method`.

**2026-10-02 change.** Using `ILogger` (`Microsoft.Extensions.Logging.Abstractions`) and `ActivitySource` directly in the provider packages, the most common pattern in .NET libraries, was reconsidered and rejected for the core packages. On .NET Framework 4.8 (via netstandard2.0, [ADR-0005](0005-target-frameworks.md)) both are extra packages (`Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`), and the maintainer doesn't want the core and provider packages to depend on them. Those packages keep to the BCL packages [ADR-0004](0004-dependency-policy.md) already allows on netstandard2.0 (`System.Text.Json`, `Microsoft.Bcl.TimeProvider`, …); `System.Diagnostics.DiagnosticSource` and `Microsoft.Extensions.Logging.Abstractions` are deliberately excluded there, even though they are official Microsoft packages. The adapter packages bring the standard behaviour to apps that want it; with DI ([ADR-0004](0004-dependency-policy.md)) they are wired automatically. The tracing adapter's name was open between `OpenUrzednik.OpenTelemetry` and `OpenUrzednik.Diagnostics`; it is `OpenUrzednik.Diagnostics`, because `ActivitySource` works with any listener, not only OpenTelemetry.

**2026-10-30 change: the 1.0 telemetry contract** ([#91](https://github.com/milten89/OpenUrzednik.NET/issues/91)). The abstractions stay, for the reason now given in the drivers: no third-party packages in Core and the providers, and on netstandard2.0 no `System.Diagnostics.DiagnosticSource`, which would reach every .NET Framework app for a feature only some use. Changes made with it, breaking for preview users:

* **Event ids.** `IOpenUrzednikLogger.Log` takes an `OpenUrzednikEventId` (`Id`, `Name`) after the level, and the logging adapter passes it on as `EventId`. The ids are fixed by the library, the same in every client of a provider, and unique within a log category. There is no hook to renumber them: apps that need their own numbering remap downstream (Serilog, OpenTelemetry).
* **`error.type`.** `error.code` was replaced by OpenTelemetry's `error.type`; `RecordErrors` now sets it too (to the first error's code), not only the `error` events. The `<provider>.http.get` span sets it for every failure, and a 404 now marks that span as failed as well.
* **`{provider}`** is `nbp`, not "NBP API". Error messages keep the display name.
* **Fallback source on .NET**, as described in the Decision Outcome. So `AddSource("OpenUrzednik.*")` is enough without DI on .NET 8 and later; on netstandard2.0 nothing changes. Core uses `ActivitySource` only in that internal code, and grants `InternalsVisibleTo` only on the .NET targets, because on netstandard2.0 the assemblies' internal polyfills would clash.

Rules from 1.0 on:

* **Stable within a major version:** source names, span names, the documented tags and their meaning, `error.type` and its values, logger categories, and event ids with their names (never renumbered or reused). Not covered: log message text and properties, the `error` and `exception` events, span status descriptions.
* **Evolution:** `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` don't change after 1.0, because the library calls them and other packages implement them. A new capability comes as a separate interface that the library checks with `is`, falling back when an implementation lacks it.
* **Cross-package internals:** on .NET, Diagnostics and the providers call Core's internal `SharedActivityTraceSource` and `ActivitySpan` through `InternalsVisibleTo`. Package dependencies are minimums ([ADR-0008](0008-branching-versioning-and-release.md)), so an app may pair a newer Core with older packages: within a major, those members are only added to, never changed or removed.
* **Sources are unversioned:** `ActivitySource` gets no version; the stability rule above takes its place.
* **Metrics are out of scope.** The library records no metrics; `HttpClient`'s own metrics cover requests. Adding metrics later needs a new decision.
