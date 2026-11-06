---
status: accepted
date: 2026-11-06
confidence: medium
decision-makers: milten89
---

# 0017. HttpClient logging in DI packages of identifier-carrying providers

## Context and Problem Statement

[ADR-0003](0003-telemetry-abstractions.md) forbids personal identifiers in logs and traces, and [ADR-0006](0006-shared-http-layer.md) gives the shared executor a display path for requests whose path or query carries one (e.g. a NIP in the MF White List's `/api/search/nip/{nip}`). The executor now writes only the display path. The `HttpClient` that a DI package registers still logs the real request URI: `IHttpClientFactory` adds two default loggers to every named client, at `Information` level. On .NET 8 they log the whole URI; from .NET 9 they replace the query with `*` but leave the path as it is ([query redaction](https://learn.microsoft.com/dotnet/core/compatibility/networking/9.0/query-redaction-logs)). How does a DI package of such a provider keep identifiers out of the factory's logging, without taking request logs away from apps?

## Decision Drivers

* ADR-0003: no identifiers in logs, whoever owns the logger, when our package creates the client that writes them.
* Apps keep the request logs they know: the same categories, event ids and messages, so their filters and queries keep working.
* An app can change the choice with one call on the builder the DI package returns.
* Little to maintain: the executor already logs every failure with the display path.
* No new package dependency ([ADR-0004](0004-dependency-policy.md)).

## Considered Options

* A logger that mirrors Microsoft's default request start and end entries, with the display URI
* Remove the factory's loggers and add none
* A logger of our own design (own event ids, messages and fields)
* Documentation only
* Set `RequestMetadata.RequestRoute` for Microsoft's extended HTTP logging

## Decision Outcome

Chosen option: "A logger that mirrors Microsoft's default request start and end entries, with the display URI", because it keeps identifiers out by default, leaves apps the same request logs, and its contract is Microsoft's, not a new one.

* The DI package of a provider whose request paths or queries carry personal identifiers calls `AddOpenUrzednikDisplayPathLogging()` on its named client. The method is internal shared source in `src/Http/DependencyInjection`, linked into the DI package. It calls `RemoveAllLoggers()` and adds two loggers in the default loggers' places: one wraps the handler pipeline, one sits next to the primary handler.
* The loggers write what Microsoft's .NET 10 default loggers write:
  * categories `System.Net.Http.HttpClient.{name}.LogicalHandler` (outside the handlers) and `System.Net.Http.HttpClient.{name}.ClientHandler` (inside), with the client's name;
  * event ids, names, levels and message templates: 100 (`RequestPipelineStart`, `RequestStart`), 101 (`RequestPipelineEnd`, `RequestEnd`) and 104 (`RequestPipelineFailed`, `RequestFailed`), at `Information`. Event 104 is written only for an `HttpRequestException`, as .NET 10 does.
* `{Uri}` is the display URI the executor attaches to the request, with a non-empty query replaced by `*`, as .NET 9 and later do. A request without one is logged the way .NET 9 and later redact it, also when an app turns that redaction off: the path is kept, a query becomes `?*`, user info and fragment are dropped. That path carries no identifier, because the provider gives a display path whenever it does.
* The executor attaches the display URI under a request option key that the provider and its DI package each compile. An app may pair different versions of the two, and a logger that finds no key logs the real path, so the key stays the same within a major, like Core's cross-package internals (ADR-0003).
* Differences from Microsoft's loggers: no `HTTP {HttpMethod} {Uri}` scope; no request and response header entries (event ids 102 and 103, `Trace`); and when a `DelegatingHandler` rewrites the request URI (a gateway, [ADR-0007](0007-client-api-and-extensibility.md)), the `ClientHandler` entry still shows the display URI built from the base address. These event ids belong to Microsoft's categories, so ADR-0003's numbering of our own events (1xxx, 2xxx) doesn't apply to them; the contract is "the same as Microsoft's default loggers".
* An app changes it on the returned builder: `.RemoveAllLoggers().AddDefaultLogger()` restores Microsoft's loggers (raw paths, the app's responsibility), and `.AddExtendedHttpClientLogging()` replaces them with Microsoft's extended logging, which redacts the path by default. App-wide defaults (`ConfigureHttpClientDefaults`, including `services.AddExtendedHttpClientLogging()`) run before a named client's configuration, so they don't override this.
* Tracing is out of a library's reach: from .NET 9, `System.Net.Http` sets `url.full` with the path kept; on .NET 8 its DiagnosticListener events carry the request; OpenTelemetry's HttpClient instrumentation keeps the path. The README of each such provider says so and how an app filters or rewrites it.
* NBP keeps the factory's default loggers: its URIs carry no identifiers.

### Consequences

* Good, because identifiers stay out of the factory's logs by default, on .NET 8 too.
* Good, because apps see the entries they know, under the same categories and ids, and can swap them with one call.
* Good, because the logger is shared source: each provider's DI package gets it with one call and one linked file.
* Bad, because we follow Microsoft's message shape: a change there (rare: the templates and ids are the same since .NET 5, apart from event 104 in .NET 10) is ours to mirror.
* Bad, because the scope and the header entries are gone for these clients, and an app that wants the real paths has to opt in.
* Bad, because traces still carry the path; only documentation covers it.

### Confirmation

* Tests of the shared logger, through a real `IHttpClientFactory` and the executor: for a request without a query, the entries equal those of Microsoft's default logger of the referenced `Microsoft.Extensions.Http` version (category, id, name, level, message; the elapsed time aside); with a display path they carry the display URI and no identifier; `ConfigureHttpClientDefaults` doesn't bring the default loggers back; one builder call does.
* Each such DI package has a test that its named client logs no identifier through the factory's logging.
* The `reviewer` agent checks that a DI package of a provider with display paths calls `AddOpenUrzednikDisplayPathLogging()`.

## Pros and Cons of the Options

### A logger that mirrors Microsoft's default request start and end entries, with the display URI

* Good, because it's safe by default and keeps the request logs apps expect.
* Neutral, because it needs the executor to attach the display URI to the request.
* Bad, because it is code to keep in line with Microsoft's.

### Remove the factory's loggers and add none

* Good, because it's one call and nothing to maintain.
* Bad, because apps lose the per-request start and end entries they rely on, unless they opt back in and log the raw path.

### A logger of our own design

* Bad, because it adds new event ids, categories and messages to the telemetry contract, breaks apps' filters, and costs more to build and keep.

### Documentation only

* Good, because it's what most libraries do.
* Bad, because the default setup would log NIPs and account numbers, against ADR-0003.

### Set `RequestMetadata.RequestRoute` for Microsoft's extended HTTP logging

* Good, because Microsoft's extended logging would show the route template instead of `REDACTED`.
* Bad, because it needs `Microsoft.Extensions.Telemetry.Abstractions` (a new dependency, ADR-0004), helps only apps that use extended logging, and nothing in tracing reads it. A possible later addition.

## More Information

* Issue [#225](https://github.com/milten89/OpenUrzednik.NET/issues/225); the display path: [#176](https://github.com/milten89/OpenUrzednik.NET/issues/176).
* Microsoft's default loggers: `LoggingHttpMessageHandler`, `LoggingScopeHttpMessageHandler` and `LogHelper` in `dotnet/runtime` (`src/libraries/Microsoft.Extensions.Http/src/Logging`); the URI redaction: `UriRedactionHelper`.
