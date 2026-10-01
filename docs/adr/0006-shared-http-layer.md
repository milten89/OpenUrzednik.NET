---
status: accepted
date: 2026-10-01
decision-makers: milten89
---

# 0006. Shared HTTP layer for REST providers

## Context and Problem Statement

`OpenUrzednik.Nbp` contains HTTP plumbing (`HttpClientExtensions.GetNbpAsync`): sending requests, mapping status codes to errors, reading `Retry-After`, deserializing JSON, recording telemetry. Future REST providers (MF Biała Lista, KRS) need exactly the same behaviour, and it must implement [ADR-0002](0002-result-pattern-and-error-handling.md) consistently (timeouts, network failures, disposal). GUS BIR uses SOAP. Where does shared HTTP code live?

## Decision Drivers

* One implementation of status mapping, timeout detection, disposal and telemetry.
* Keep `OpenUrzednik.Core` small, stable and free of HTTP/JSON concerns.
* Avoid needless version bumps: every provider's `version.json` path-filters its dependencies.

## Considered Options

* New package `OpenUrzednik.Http` for REST/JSON providers
* New package with pluggable deserialization (JSON and SOAP/XML)
* Shared source files compiled into each provider
* Put it into `OpenUrzednik.Core`

## Decision Outcome

Chosen option: "New package `OpenUrzednik.Http` for REST/JSON providers".

* `OpenUrzednik.Http` depends on `OpenUrzednik.Core` and contains the request executor: send, status → error mapping (with provider-specific overrides, e.g. NBP's 400 messages), timeout vs caller-cancellation detection, `Retry-After` parsing with `TimeProvider`, JSON deserialization via `JsonTypeInfo<T>` (source-generated, trimming/AOT friendly), disposal of request/response, telemetry spans and logs.
* It has a public API (needed across assemblies) and follows SemVer. Types intended only for provider authors live in a clearly named namespace (e.g. `OpenUrzednik.Http.Infrastructure`) and are documented as such.
* `OpenUrzednik.Nbp` is migrated to it first; NBP-specific behaviour stays in NBP.
* GUS (SOAP) builds its own stack; if real duplication appears later, a new ADR can extract a transport-only layer.

### Consequences

* Good, because Core stays pure (results, errors, telemetry abstractions, validation).
* Good, because only REST providers are re-versioned when the HTTP layer changes.
* Bad, because one more package must be versioned and published.
* Bad, because its public surface must be kept stable for providers built on it.

### Confirmation

Provider packages contain no direct `HttpClient.SendAsync` calls; all HTTP goes through `OpenUrzednik.Http`. `version.json` of each REST provider includes `../OpenUrzednik.Http` in `pathFilters`.

## Pros and Cons of the Options

### Put it into `OpenUrzednik.Core`

* Good, because there is one package fewer.
* Bad, because Core gains HTTP/JSON code (and `System.Text.Json` on netstandard2.0) that SOAP providers do not need.
* Bad, because every HTTP fix bumps every provider (all path-filter Core).

### Shared source files

* Good, because there is no extra public API or package.
* Bad, because fixes require re-releasing each provider, and code is duplicated across binaries.
