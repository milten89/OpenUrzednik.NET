---
status: released
released-in: v1.0.0
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0007. Client API: simple defaults, internal request shaping

## Context and Problem Statement

A client needs an `HttpClient` and a way to build the request URLs. URL building is an implementation detail: if it is a required constructor argument (`new NbpGoldPriceClient(httpClient, new NbpUrlBuilderFactory())`), every consumer has to know about it. Most consumers just want defaults, but companies sometimes call public APIs through internal proxies or gateways with a different base URL, path prefix or additional query parameters. How do we keep the common case trivial and the unusual case possible?

## Decision Drivers

* The default scenario needs no knowledge of internals.
* Unusual network setups (proxies, gateways with other paths or query parameters) work without forking.
* Extension points are explicit, documented and stable; the public surface has no more of them than these setups need.

## Considered Options

* Simple constructors with optional, replaceable components (interfaces with default implementations)
* Simple constructors; request shaping internal, HTTP customization through `DelegatingHandler`s
* Single facade client (`NbpClient.Currency/.Tables/.Gold`)
* A required URL-builder factory parameter

## Decision Outcome

Chosen option: "Simple constructors; request shaping internal, HTTP customization through `DelegatingHandler`s", because a handler covers every gateway case that public URL-builder interfaces would cover, without making those interfaces API that must stay stable after 1.0.

* **Default path:** `new NbpGoldPriceClient(httpClient)` works with defaults (base URL from `NbpOptions.DefaultApiUrl` when `HttpClient.BaseAddress` is not set). DI packages give `services.AddOpenUrzednikNbp()`.
* **Configuration:** a plain options object (`NbpOptions`: base URL, timeout, …); no `IOptions<T>` in provider packages ([ADR-0004](0004-dependency-policy.md)).
* **Request shaping is internal:** the URL builders (`INbpUrlBuilderFactory`, `INbpUrlBuilder`, their defaults and `NbpTable`) are internal. Tests replace them through an internal constructor, and NSubstitute mocks them through `InternalsVisibleTo("DynamicProxyGenAssembly2")`. A gateway's base address goes into `NbpOptions.ApiUrl`; renamed paths and extra query parameters are rewritten in a `DelegatingHandler` (the NBP README shows one).
* **Cross-cutting HTTP concerns** (headers, auth, proxies, retries, path rewriting) are done with `DelegatingHandler`s / `HttpClient` configuration, not with library-specific hooks. Auth that needs protocol calls (a session login such as GUS `Zaloguj` returning a session id, token acquisition) or credentials passed as API arguments belongs in the provider and is designed in its provider ADR ([ADR-0010](0010-provider-readiness-gate.md)); whether a static API key is a handler or a provider option is decided there too.
* Separate clients per API area (currency, tables, gold) remain; a facade may be added later without breaking changes.
* **Namespace layout:** what an application uses to call a provider is in the provider's root namespace (`OpenUrzednik.Nbp`: the clients, their interfaces, `NbpOptions`, `NbpTelemetry`), so one `using` is enough for the common case; the clients' files live in the project root. Models and enums stay in the namespace of the area that defines them (`OpenUrzednik.Nbp.Currency`, `.Table`, `.Gold`; e.g. `TableType` is in `.Table`, also for the currency client). In Core, the result types and their extensions on `Task<…>` (`EnsureSuccessAsync`, `MapAsync`, `BindAsync`) are in `OpenUrzednik.Core`, and operations on a result are instance methods (`EnsureSuccess`, `And`, `Map`, `Bind`, `Match`); errors, exceptions, telemetry and validation have their own namespaces (`.Errors`, `.Exceptions`, `.Telemetry`, `.Validation`), with extension methods next to the type they extend (`RecordError` in `.Telemetry`). No public namespace of Core or a provider is named after a kind of member (`.Extensions`, `.Options`); package names modelled on `Microsoft.Extensions.*` (`OpenUrzednik.Extensions.Logging`) and internal namespaces are exempt. DI registration stays in `Microsoft.Extensions.DependencyInjection`, as is usual for `IServiceCollection` extensions.
* **Constructors:** each client has two public constructors, one with only the `HttpClient` and one with the optional `NbpOptions`, `TimeProvider`, logger and trace source. DI packages create the clients through the second one in a factory; a bare `AddHttpClient<TClient>()` isn't supported, because the typed-client activator finds no constructor it can use.

### Consequences

* Good, because the first-use experience is one line.
* Good, because unusual network setups are supported without forking.
* Good, because the public surface has no extension interfaces to keep stable.
* Bad, because a gateway that renames paths needs a handler that knows the NBP paths, and a change of those paths in a new version can break it.

### Confirmation

README shows the one-line default usage and the gateway handler, with only the namespaces of the layout above; tests construct clients with defaults and, through the internal constructor, with substituted URL builders. The `reviewer` agent flags a public type or constructor parameter that shapes requests.

## Pros and Cons of the Options

### Simple constructors with optional, replaceable components

Public interfaces with public defaults, passed as optional constructor parameters (e.g. `INbpUrlBuilderFactory` for custom base URLs, paths or extra query parameters).

* Good, because a gateway can change base URLs, paths or query parameters without a handler.
* Bad, because every such interface is a long-term compatibility commitment, and a `DelegatingHandler` plus `NbpOptions.ApiUrl` covers the same gateway cases.

### A required URL-builder factory parameter

* Bad, because an implementation detail (URL building) is a required constructor argument, so the default case needs knowledge of internals.

## More Information

### Changes before 1.0

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0007-client-api-and-extensibility.md#more-information).

* 2026-10-29 · [#86](https://github.com/milten89/OpenUrzednik.NET/issues/86) · Request shaping became internal: the public URL-builder interfaces and the constructor parameter were removed; gateways use `NbpOptions.ApiUrl` and a `DelegatingHandler`.
* 2026-10-29 · [#87](https://github.com/milten89/OpenUrzednik.NET/issues/87) · Namespace layout: clients, their interfaces and `NbpOptions` in the provider's root namespace; Core's result operations as instance methods; no `.Extensions` or `.Options` namespaces.
* 2026-10-30 · [#109](https://github.com/milten89/OpenUrzednik.NET/issues/109) · Auth that needs protocol calls or credentials passed as API arguments belongs in the provider, designed in its provider ADR.
