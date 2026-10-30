---
status: accepted
date: 2026-10-30
decision-makers: milten89
---

# 0007. Client API: simple defaults, internal request shaping

## Context and Problem Statement

Originally, a consumer had to construct `new NbpGoldPriceClient(httpClient, new NbpUrlBuilderFactory())` – an implementation detail (URL building) is a required constructor argument. Most consumers just want defaults, but companies sometimes call public APIs through internal proxies/gateways with a different base URL, path prefix or additional query parameters. How do we keep the common case trivial and the unusual case possible?

## Decision Drivers

* The default scenario needs no knowledge of internals.
* Unusual network setups (proxies, gateways with other paths or query parameters) work without forking.
* Extension points are explicit, documented and stable; the public surface has no more of them than these setups need.

## Considered Options

* Simple constructors with optional, replaceable components (interfaces with default implementations)
* Simple constructors; request shaping internal, HTTP customization through `DelegatingHandler`s
* Single facade client (`NbpClient.Currency/.Tables/.Gold`)
* Keep current shape (required factory parameter)

## Decision Outcome

Chosen option: "Simple constructors; request shaping internal, HTTP customization through `DelegatingHandler`s", because a handler covers every gateway case the public URL-builder interfaces covered, without making those interfaces API that must stay stable after 1.0.

* **Default path:** `new NbpGoldPriceClient(httpClient)` works with defaults (base URL from `NbpOptions.DefaultApiUrl` when `HttpClient.BaseAddress` is not set). DI packages give `services.AddOpenUrzednikNbp()`.
* **Configuration:** a plain options object (`NbpOptions`: base URL, timeout, …) – no `IOptions<T>` in provider packages ([ADR-0004](0004-dependency-policy.md)).
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

## More Information

**2026-10-29 change: request shaping is internal** ([#86](https://github.com/milten89/OpenUrzednik.NET/issues/86)). Before, components that shaped requests were public interfaces with public defaults, passed as optional constructor parameters (`INbpUrlBuilderFactory` for custom base URLs, paths or extra query parameters), and the DI package used a registered `INbpUrlBuilderFactory`. The maintainer made them internal before 1.0: every such interface would have been a long-term compatibility commitment, and a `DelegatingHandler` plus `NbpOptions.ApiUrl` covers the same gateway cases. The `urlBuilderFactory` constructor parameter and the DI pickup were removed, and the title ("simple defaults, replaceable internals"), the chosen option, drivers, consequences and Confirmation were rewritten. Both public constructors stay.

**2026-10-29 change: namespace layout** ([#87](https://github.com/milten89/OpenUrzednik.NET/issues/87)). Before, the clients, their interfaces and `NbpOptions` were in the area namespaces (`OpenUrzednik.Nbp.Currency`, `.Table`, `.Gold`, `.Options`), so the one-line default needed a `using` per area, and Core's `EnsureSuccess`, `And`, `EnsureSuccessAsync`, `MapAsync`, `BindAsync`, `RecordError` and `RecordErrors` were extension methods in `OpenUrzednik.Core.Extensions`. The maintainer settled the layout above before 1.0, when moving types is still allowed. The clients' logger category (their full type name) changed with them, e.g. to `OpenUrzednik.Nbp.NbpGoldPriceClient`; a filter on the `OpenUrzednik.Nbp` prefix still matches, but filters on `OpenUrzednik.Nbp.Currency`, `.Table` or `.Gold` no longer match the clients and change their log level silently; use the type name or `OpenUrzednik.Nbp`.

**2026-10-30 change: auth that a handler can't do** ([#109](https://github.com/milten89/OpenUrzednik.NET/issues/109)). The rule named auth as a handler concern, which fits a static header but not a session login that is itself an API call, or credentials that are arguments of the API (GUS BIR). Those stay in the provider, and each provider ADR decides how, including for a static API key.
