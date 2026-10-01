---
status: accepted
date: 2026-10-01
decision-makers: milten89
---

# 0007. Client API: simple defaults, replaceable internals

## Context and Problem Statement

Today a consumer must construct `new NbpGoldPriceClient(httpClient, new NbpUrlBuilderFactory())` – an implementation detail (URL building) is a required constructor argument. Most consumers just want defaults, but companies sometimes call public APIs through internal proxies/gateways with a different base URL, path prefix or additional query parameters. How do we keep the common case trivial and the unusual case possible?

## Decision Drivers

* The default scenario needs no knowledge of internals.
* Anything the library builds (URLs, requests) can be replaced by the consumer when needed.
* Extension points are explicit, documented and stable.

## Considered Options

* Simple constructors with optional, replaceable components (interfaces with default implementations)
* Single facade client (`NbpClient.Currency/.Tables/.Gold`)
* Keep current shape (required factory parameter)

## Decision Outcome

Chosen option: "Simple constructors with optional, replaceable components".

* **Default path:** `new NbpGoldPriceClient(httpClient)` works with defaults (base URL from `NbpOptions.DefaultApiUrl` when `HttpClient.BaseAddress` is not set). DI packages give `services.AddOpenUrzednikNbp()`.
* **Configuration:** a plain options object (`NbpOptions`: base URL, timeout, …) – no `IOptions<T>` in provider packages ([ADR-0004](0004-dependency-policy.md)).
* **Replacement:** components that shape requests are public interfaces with public default implementations, accepted as optional constructor parameters, e.g. `INbpUrlBuilderFactory` for custom base URLs, paths or extra query parameters. Default implementations are reusable by composition (decorate the default rather than re-implement).
* **Cross-cutting HTTP concerns** (headers, auth, proxies, retries) are done with `DelegatingHandler`s / `HttpClient` configuration, not with library-specific hooks.
* Separate clients per API area (currency, tables, gold) remain; a facade may be added later without breaking changes.
* Constructor overloads are kept minimal: one with only required parameters, one with all optional components (`TimeProvider`, logger, tracer, builders).

### Consequences

* Good, because the first-use experience is one line.
* Good, because unusual network setups are supported without forking.
* Bad, because every public extension interface is a long-term compatibility commitment.

### Confirmation

README shows the one-line default usage; tests construct clients with defaults and with custom implementations of each extension interface.
