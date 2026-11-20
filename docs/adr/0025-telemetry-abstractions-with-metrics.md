---
status: accepted
date: 2026-11-20
confidence: medium
decision-makers: milten89
---

# 0025. Telemetry abstractions and adapter packages, with per-operation metrics

## Context and Problem Statement

Clients log and trace their work. The common .NET choice is `Microsoft.Extensions.Logging.ILogger` and `System.Diagnostics.ActivitySource`, but `ILogger` is a NuGet dependency, and on netstandard2.0 so is `ActivitySource` (`System.Diagnostics.DiagnosticSource`). The alternative is the library's own abstractions in Core (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource`, `IOpenUrzednikSpan`). Which should the clients use?

This record restates [ADR-0003](0003-telemetry-abstractions.md), released in v1.0.0, and supersedes it ([#209](https://github.com/milten89/OpenUrzednik.NET/issues/209)). The one change is metrics, which ADR-0003 left out. `HttpClient`'s own metrics (`http.client.request.duration`, .NET 8 and later) have no path or operation tag, stop at the response headers, count each retry on its own, can't see a failure inside a successful response (a per-entry error, a body that doesn't deserialize, which 429 it was), and don't exist on .NET Framework. Apps with several providers, and a provider with a daily quota such as the White List, need duration, call count and failures per operation. How do the clients record them?

## Decision Drivers

* Core and provider packages depend on no third-party packages; on netstandard2.0 they use only the Microsoft BCL packages [ADR-0004](0004-dependency-policy.md) allows. `System.Diagnostics.DiagnosticSource` and `Microsoft.Extensions.Logging.Abstractions` stay excluded there, even though they are official Microsoft packages; `System.Diagnostics.DiagnosticSource` would add a package to every .NET Framework app for a feature only some use.
* Consumers using `ILogger`/OpenTelemetry must not have to write adapters themselves.
* Zero overhead when telemetry is not configured.
* Metrics: the same pattern as logging and tracing (an abstraction in Core, an adapter in a package), netstandard2.0 included; room for the metrics later providers need without breaking custom implementations; one timing per operation, equal to the trace's when both exist.

## Considered Options

* Keep custom abstractions and ship official adapter packages
* Use `ILogger` + `ActivitySource` directly
* Hybrid: `ActivitySource` directly, custom logger

For metrics:

* No metrics: rely on `HttpClient`'s metrics and derive the rest from traces
* A `Meter` in shared source on the .NET targets only
* Metrics recorded by the trace source's spans
* An abstract metrics class in Core and a `Meter` adapter in `OpenUrzednik.Diagnostics`
* A metrics interface in Core and a `Meter` adapter in `OpenUrzednik.Diagnostics`

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
* **Metrics** follow the section below; their meter names, instrument names, units and documented tags are stable within a major, like the other names above.

### Metrics

For metrics the chosen option is "An abstract metrics class in Core and a `Meter` adapter in `OpenUrzednik.Diagnostics`".

* **The abstraction:** Core's `public abstract class OpenUrzednikMetrics` with one virtual, empty method per kind of measurement, starting with `RecordOperation(OpenUrzednikOperationMeasurement measurement)`. The measurement is a `readonly struct` passed by value (no `in`, so every .NET language can override the method): the operation (its span name, e.g. `nbp.gold.latest`), the provider's short name, the duration, `error.type` on failure, and the server address and port. A virtual `IsEnabled` property (`true` by default) lets the operation code skip the measurement when nothing records it; `NullOpenUrzednikMetrics` returns `false` and records nothing. A class, not an interface: a minor version adds a measurement as a new virtual method with an empty body, so a custom implementation keeps compiling and simply doesn't record it until it overrides the method; ADR-0011 forbids only new abstract members on extension points, and `OpenUrzednikMetrics` joins them. Default interface methods would do the same, but netstandard2.0 and .NET Framework don't support them. The measurement types are created by the library, so a minor version may add properties to them.
* **Likely later measurements** (each a virtual method in a minor, decided with its feature): the HTTP request including the body read, batch sizes and per-entry errors, payload and download sizes, cache hits and misses, flat-file store rebuilds and lookups and the data's age, session logins of SOAP providers, operations in flight. They map to histograms, counters, up-down counters and gauges.
* **The adapter and the meters:** `OpenUrzednik.Diagnostics` gets `MeterMetrics : OpenUrzednikMetrics` over `System.Diagnostics.Metrics.Meter`, on every target (netstandard2.0 through the `System.Diagnostics.DiagnosticSource` package it already references). There is one meter per provider for the process, named like its trace source (`OpenUrzednik.<Provider>`), so OpenTelemetry users subscribe with `AddMeter("OpenUrzednik.*")`: as with tracing, the meter code lives in Core's internal telemetry files, compiled into Core on the .NET targets and into `OpenUrzednik.Diagnostics` for netstandard2.0, and `MeterMetrics.GetShared(name)` returns the same shared instance that Core's .NET default uses, so a process never holds two meters with one name. The meters are static, not created through `IMeterFactory`, because that type is .NET-only and the public API is the same on every target. The first instrument is the histogram `openurzednik.client.operation.duration`, in seconds, with the tags `openurzednik.operation` (the span name), `openurzednik.provider`, `error.type` (only on failure, `OpenUrzednikError.Code`), `server.address` and `server.port`. Its bucket advice is OpenTelemetry's RPC set (0.0001 s to 60 s) where `InstrumentAdvice` exists: on net10.0, and on netstandard2.0 through the DiagnosticSource package (9.0 or later), but not on net8.0, whose built-in DiagnosticSource is 8.0. `IsEnabled` follows `Instrument.Enabled`, so nothing is measured while nothing listens. Tags never carry identifiers or secrets (the rule above).
* **Defaults:** a client without metrics uses `NullOpenUrzednikMetrics`; on the .NET targets, as with tracing, the shared meter in Core's internals records the same instruments, so `AddMeter("OpenUrzednik.*")` works without DI. DI packages pass `MeterMetrics.GetShared(...)`.
* **Clients:** each client gets a new constructor with `metrics = null` after `traceSource`. The constructor it extends, the one with the optional `timeProvider`, `logger` and `traceSource`, loses its default values, so a call with some of the optional arguments (`new X(httpClient, options)`) binds to the new constructor instead of being ambiguous (CS0121). In 1.1.0 the old constructor is marked `[Obsolete]` (telling the caller to pass `metrics`, or `null`) and `[EditorBrowsable(EditorBrowsableState.Never)]`, and 2.0 removes it. Constructors with only required parameters, such as `(HttpClient)`, stay. A provider not released yet gets only the new constructor.
* **Timing, exactly once:** the shared operation code in `src/Http` records the measurement while the operation's span is still current, so a metric can link to its trace (exemplars take `Activity.Current`). A span that implements the capability interface `IOpenUrzednikSpanTiming` (the internal `ActivitySpan` does) fixes its end time and returns its duration (`TimeSpan? StopTiming()`: `Activity.SetEndTime`, then `Activity.Duration`); the measurement takes exactly that value and the span then ends, keeping that end time, so a metric and its trace never disagree. Without one (the trace isn't sampled, nobody listens, or a custom trace source), the operation code measures once itself, with two `Stopwatch` timestamps. `IOpenUrzednikSpan` stays unchanged.

### Consequences

* Good, because packages stay dependency-free and have the same API on netstandard2.0 and .NET; only the .NET default trace source differs.
* Good, because consumers of `ILogger`/OpenTelemetry get first-party adapters.
* Bad, because the abstractions must be kept expressive enough (structured properties, events, exceptions) and versioned carefully.
* Bad, because two more packages must be maintained.
* Good, because every operation gets duration, call count and failures by `error.type` on every target, and the metric's duration equals the trace's.
* Good, because new measurements arrive in minor versions without breaking custom `OpenUrzednikMetrics` implementations.
* Bad, because every released client gets another constructor, and a caller that passes every argument of the old one (options, `timeProvider`, logger and trace source) sees an obsolete warning until it adds `metrics` (or `null`); 2.0 removes the old constructors.
* Bad, because the measurement types and the instrument names become part of the stable contract.

### Confirmation

Adapter packages have tests asserting that spans/log entries produced by a client reach `ILogger`/`ActivityListener` (`NbpClientLoggingTest`, `NbpClientTracingTest`, including the .NET fallback source); `NbpLogEventsTest` checks that `NbpLogEvents` matches the events the clients log.

Metrics: tests with a `MeterListener` per client assert the instrument, its unit and tags, and with an `ActivityListener` as well, that the recorded duration equals `Activity.Duration` and that the measurement is taken while the span is `Activity.Current`; a test without a sampled trace checks the operation code's own measurement. `tests/OpenUrzednik.Architecture.Tests` checks that `OpenUrzednikMetrics` has no abstract member, that every public client has a constructor with a metrics parameter, and that an `[Obsolete]` constructor has no optional parameters (`OptionalParametersTest` skips obsolete members, but overload resolution doesn't). The `reviewer` agent flags a renamed instrument, meter or tag outside a major, an identifier in a tag, and a new abstract member on `OpenUrzednikMetrics`.

## Pros and Cons of the Options

### Use `ILogger` + `ActivitySource` directly

* Good, because it is the most common pattern in .NET libraries.
* Bad, because on .NET Framework (through netstandard2.0, [ADR-0005](0005-target-frameworks.md)) both are extra packages (`Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`) in the core and provider packages. The adapter packages bring the standard behaviour to apps that want it, and DI wires them automatically.

### No metrics

* Good, because nothing changes and the contract stays as it is.
* Bad, because `HttpClient`'s metrics can't tell operations apart on one host (the White List's searches, checks and status calls), miss failures inside a successful response, and don't exist on .NET Framework; deriving metrics from traces needs every trace sampled and a collector connector.

### A `Meter` in shared source on the .NET targets only

* Good, because it needs no public API and no dependency (the Npgsql pattern).
* Bad, because netstandard2.0 gets no metrics and custom back ends can't plug in.

### Metrics recorded by the trace source's spans

* Good, because it needs no client changes: the internal span code records the histogram, on netstandard2.0 through `ActivityTraceSource`.
* Bad, because metrics depend on tracing being set up, a custom trace source gets none, and there is no contract to implement.

### An abstract metrics class in Core and a `Meter` adapter

* Good, because it follows the pattern of the other telemetry abstractions, works on every target through `OpenUrzednik.Diagnostics`, and grows in minor versions without breaking custom implementations.
* Bad, because the released clients need new constructors, and the old ones go through an obsolete period before 2.0.

### A metrics interface in Core

* Good, because it matches the other telemetry abstractions.
* Bad, because an interface is frozen after 1.0, so every later measurement would need another interface, which custom implementations would have to add one by one; a generic meter interface is a large surface to get right at once.

## More Information

* Supersedes [ADR-0003](0003-telemetry-abstractions.md); everything except the metrics is restated from it unchanged.
* Sources for the metrics decision: [.NET's built-in networking metrics](https://learn.microsoft.com/dotnet/core/diagnostics/built-in-metrics-system-net), [creating metrics](https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation), OpenTelemetry's [RPC](https://opentelemetry.io/docs/specs/semconv/rpc/rpc-metrics/) and [database](https://opentelemetry.io/docs/specs/semconv/database/database-metrics/) client metrics, Npgsql's `MetricsReporter` and Polly's telemetry listener.
* The implementation is split into issues opened with this record.

### Changes before 1.0 (of ADR-0003)

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0003-telemetry-abstractions.md#more-information).

* 2026-10-02 · backlog item 16 · Both adapter packages were implemented, and the HTTP tags took OpenTelemetry's names (`url.path`, `http.response.status_code`, `http.request.method`).
* 2026-10-02 · Using `ILogger` and `ActivitySource` directly was reconsidered and rejected; the tracing adapter was named `OpenUrzednik.Diagnostics`.
* 2026-10-30 · [#91](https://github.com/milten89/OpenUrzednik.NET/issues/91) · The 1.0 telemetry contract: event ids, `error.type`, the short `{provider}` name, the .NET fallback source and the stability rules.
* 2026-10-30 · [#109](https://github.com/milten89/OpenUrzednik.NET/issues/109) · The rule against secrets and personal identifiers covers every telemetry and error channel and every holder of an identifier.
* 2026-11-05 · [#121](https://github.com/milten89/OpenUrzednik.NET/issues/121) · The HTTP span carries `server.address` and `server.port`.
