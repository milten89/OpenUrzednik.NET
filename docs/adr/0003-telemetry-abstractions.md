---
status: accepted
date: 2026-10-01
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
  * `OpenUrzednik.OpenTelemetry` (or `OpenUrzednik.Diagnostics`) – `IOpenUrzednikTraceSource` over `ActivitySource`, with documented source names.
* DI packages ([ADR-0004](0004-dependency-policy.md)) wire the adapters automatically when the consumer registers logging/tracing.

**Conventions:**

* Span names: `<provider>.<area>.<operation>` in snake_case, e.g. `nbp.currency.buy_sell_latest`, `nbp.http.get`.
* Tag names: `<provider>.<parameter>` (e.g. `nbp.currency`, `nbp.top_count`); standard HTTP tags follow OpenTelemetry semantic conventions (`http.status_code` → prefer `http.response.status_code` when adapters are introduced).
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

Adapters are not implemented yet (see `docs/BACKLOG.md`).
