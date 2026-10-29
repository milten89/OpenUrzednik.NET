---
status: accepted
date: 2026-10-29
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
* **Cross-cutting HTTP concerns** (headers, auth, proxies, retries, path rewriting) are done with `DelegatingHandler`s / `HttpClient` configuration, not with library-specific hooks.
* Separate clients per API area (currency, tables, gold) remain; a facade may be added later without breaking changes.
* **Constructors:** each client has two public constructors, one with only the `HttpClient` and one with the optional `NbpOptions`, `TimeProvider`, logger and trace source. DI packages create the clients through the second one in a factory; a bare `AddHttpClient<TClient>()` isn't supported, because the typed-client activator finds no constructor it can use.

### Consequences

* Good, because the first-use experience is one line.
* Good, because unusual network setups are supported without forking.
* Good, because the public surface has no extension interfaces to keep stable.
* Bad, because a gateway that renames paths needs a handler that knows the NBP paths, and a change of those paths in a new version can break it.

### Confirmation

README shows the one-line default usage and the gateway handler; tests construct clients with defaults and, through the internal constructor, with substituted URL builders. The `reviewer` agent flags a public type or constructor parameter that shapes requests.

## More Information

**2026-10-29 change: request shaping is internal** ([#86](https://github.com/milten89/OpenUrzednik.NET/issues/86)). Before, components that shaped requests were public interfaces with public defaults, passed as optional constructor parameters (`INbpUrlBuilderFactory` for custom base URLs, paths or extra query parameters), and the DI package used a registered `INbpUrlBuilderFactory`. The maintainer made them internal before 1.0: every such interface would have been a long-term compatibility commitment, and a `DelegatingHandler` plus `NbpOptions.ApiUrl` covers the same gateway cases. The `urlBuilderFactory` constructor parameter and the DI pickup were removed, and the title ("simple defaults, replaceable internals"), the chosen option, drivers, consequences and Confirmation were rewritten. Both public constructors stay.
