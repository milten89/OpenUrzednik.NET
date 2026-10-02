---
status: accepted
date: 2026-10-02
decision-makers: milten89
---

# 0003. Telemetry abstractions and adapter packages

## Context and Problem Statement

Clients log and trace their work. The common .NET choice is `Microsoft.Extensions.Logging.ILogger` and `System.Diagnostics.ActivitySource`, but `ILogger` is a NuGet dependency, and on netstandard2.0 so is `ActivitySource` (`System.Diagnostics.DiagnosticSource`). The library already defines its own `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` in Core. Should we keep them or switch to the standard types?

## Decision Drivers

* Core and provider packages carry no third-party or Microsoft.Extensions dependencies ([ADR-0004](0004-dependency-policy.md)).
* Consumers using `ILogger`/OpenTelemetry must not have to write adapters themselves.
* Zero overhead when telemetry is not configured.

## Considered Options

* Keep custom abstractions and ship official adapter packages
* Use `ILogger` + `ActivitySource` directly
* Hybrid: `ActivitySource` directly, custom logger

## Decision Outcome

Chosen option: "Keep custom abstractions and ship official adapter packages".

* Core owns `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource`, `IOpenUrzednikSpan` plus `Null*` implementations used when nothing is configured.
* Official adapters are separate packages, for example:
  * `OpenUrzednik.Extensions.Logging` – `IOpenUrzednikLogger` over `ILogger`.
  * `OpenUrzednik.Diagnostics` – `IOpenUrzednikTraceSource` over `ActivitySource`. Source names are `OpenUrzednik.<Provider>` (e.g. `OpenUrzednik.Nbp`), so OpenTelemetry users subscribe with `AddSource("OpenUrzednik.*")`. It depends only on `System.Diagnostics.DiagnosticSource`, and only on netstandard2.0; no dependency on the OpenTelemetry packages.
* DI packages ([ADR-0004](0004-dependency-policy.md)) wire the adapters automatically when the consumer registers logging/tracing.

**Conventions:**

* Span names: `<provider>.<area>.<operation>` in snake_case, e.g. `nbp.currency.buy_sell_latest`, `nbp.http.get`.
* Tag names: `<provider>.<parameter>` (e.g. `nbp.currency`, `nbp.top_count`); standard HTTP tags follow OpenTelemetry semantic conventions (`http.request.method`, `url.path`, `http.response.status_code`).
* Errors are recorded via `RecordError`/`RecordErrors` extensions with `error.code` = `OpenUrzednikError.Code`.
* Log calls at `Debug`/`Trace` are guarded with `IsEnabled`; messages use templates with named placeholders, never string interpolation.
* Never log secrets (API keys, session ids) or full personal identifiers (PESEL, NIP of natural persons) – mask them.

### Consequences

* Good, because packages stay dependency-free and work identically on netstandard2.0 and .NET.
* Good, because consumers of `ILogger`/OpenTelemetry get first-party adapters.
* Bad, because the abstractions must be kept expressive enough (structured properties, events, exceptions) and versioned carefully.
* Bad, because two more packages must be maintained.

### Confirmation

Adapter packages have tests asserting that spans/log entries produced by a client reach `ILogger`/`ActivityListener`.

## More Information

**2026-10-02 implementation note.** Both adapters exist (backlog item 16):

* `OpenUrzednik.Extensions.Logging`: `OpenUrzednikLogger` wraps an `ILogger`. The state lists the named values followed by `{OriginalFormat}`, as `LoggerMessage` does, so structured providers keep the properties. `loggerFactory.CreateOpenUrzednikLogger<TClient>()` uses the client's full type name as the category (as `ILogger<T>` does), so `"OpenUrzednik.Nbp"` filters a whole provider.
* `OpenUrzednik.Diagnostics`: `ActivityTraceSource` wraps an `ActivitySource`; `ActivityTraceSource.GetShared(name)` keeps one source per name for the process. Providers publish their source name as a constant (`NbpTelemetry.SourceName`). With no listener, or when the span isn't sampled, it returns the no-op span. Every span is `ActivityKind.Internal`, including `<provider>.http.get`: `HttpClient`'s own instrumentation creates the `Client` span under it, so the abstraction needs no `ActivityKind`. `RecordException` adds an `exception` event with the semantic-convention tags.
* The HTTP tags were renamed at the same time: `http.path` → `url.path` (now the absolute path, e.g. `/api/cenyzlota`), `http.status_code` → `http.response.status_code`, plus `http.request.method`.

**2026-10-02 change.** Using `ILogger` (`Microsoft.Extensions.Logging.Abstractions`) and `ActivitySource` directly in the provider packages, the most common pattern in .NET libraries, was reconsidered and rejected for the core packages. On .NET Framework 4.8 (via netstandard2.0, [ADR-0005](0005-target-frameworks.md)) both are extra packages (`Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`), and the maintainer doesn't want the core and provider packages to depend on them. Those packages keep to the BCL packages [ADR-0004](0004-dependency-policy.md) already allows on netstandard2.0 (`System.Text.Json`, `Microsoft.Bcl.TimeProvider`, …); `System.Diagnostics.DiagnosticSource` and `Microsoft.Extensions.Logging.Abstractions` are deliberately excluded there, even though they are official Microsoft packages. The adapter packages bring the standard behaviour to apps that want it; with DI ([ADR-0004](0004-dependency-policy.md)) they are wired automatically. The tracing adapter's name was open between `OpenUrzednik.OpenTelemetry` and `OpenUrzednik.Diagnostics`; it is `OpenUrzednik.Diagnostics`, because `ActivitySource` works with any listener, not only OpenTelemetry.
