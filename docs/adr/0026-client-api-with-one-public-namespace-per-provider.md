---
status: accepted
date: 2026-11-21
confidence: high
decision-makers: milten89
---

# 0026. Client API: simple defaults, internal request shaping, one public namespace per provider

This record supersedes [ADR-0007](0007-client-api-and-extensibility.md) and restates its whole decision. The only change is the namespace layout: a provider's models and enums move from area namespaces into the provider's root namespace.

## Context and Problem Statement

A client needs an `HttpClient` and a way to build the request URLs. URL building is an implementation detail: if it is a required constructor argument (`new NbpGoldPriceClient(httpClient, new NbpUrlBuilderFactory())`), every consumer has to know about it. Most consumers just want defaults, but companies sometimes call public APIs through internal proxies or gateways with a different base URL, path prefix or additional query parameters. How do we keep the common case trivial and the unusual case possible, and which namespaces does a caller need to import?

ADR-0007 put a provider's models and enums in area namespaces (`OpenUrzednik.Nbp.Currency`, `.Table`, `.Gold`), with a type that several areas use staying in the area that defines it. The MF White List provider ([ADR-0023](0023-mf-white-list-provider.md)) showed the cost: its status client returns the `VatStatus` enum of the search area, so a caller of `WhiteListStatusClient` needed `using OpenUrzednik.MF.WhiteList.Search;` for one enum, and three of the provider's areas held a single public type.

## Decision Drivers

* The default scenario needs no knowledge of internals, and one `using` is enough for it.
* Unusual network setups (proxies, gateways with other paths or query parameters) work without forking.
* Extension points are explicit, documented and stable; the public surface has no more of them than these setups need.
* A type's namespace doesn't depend on which areas happen to share it, so adding a client never moves a type (a namespace move is a breaking change, ADR-0011).
* Packages are small: about 25 to 35 public types each, about 10 of them models.

## Considered Options

* Simple constructors with optional, replaceable components (interfaces with default implementations)
* Simple constructors; request shaping internal, HTTP customization through `DelegatingHandler`s
* Single facade client (`NbpClient.Currency/.Tables/.Gold`)
* A required URL-builder factory parameter

For the namespace layout:

* Models and enums in area namespaces (ADR-0007)
* Every public type in the provider's root namespace
* The root namespace plus one `.Models` namespace
* A shared namespace for the types several areas use, plus area namespaces

## Decision Outcome

Chosen option: "Simple constructors; request shaping internal, HTTP customization through `DelegatingHandler`s", because a handler covers every gateway case that public URL-builder interfaces would cover, without making those interfaces API that must stay stable after 1.0. For the namespaces: "Every public type in the provider's root namespace", because the packages are small enough that one namespace stays readable, a caller needs one `using`, and no type ever has to be placed by which areas share it.

* **Default path:** `new NbpGoldPriceClient(httpClient)` works with defaults (base URL from `NbpOptions.DefaultApiUrl` when `HttpClient.BaseAddress` is not set). DI packages give `services.AddOpenUrzednikNbp()`.
* **Configuration:** a plain options object (`NbpOptions`: base URL, timeout, …); no `IOptions<T>` in provider packages ([ADR-0024](0024-dependency-policy-with-a-7z-reader.md)).
* **Request shaping is internal:** the URL builders (`INbpUrlBuilderFactory`, `INbpUrlBuilder`, their defaults and `NbpTable`) are internal. Tests replace them through an internal constructor, and NSubstitute mocks them through `InternalsVisibleTo("DynamicProxyGenAssembly2")`. A gateway's base address goes into `NbpOptions.ApiUrl`; renamed paths and extra query parameters are rewritten in a `DelegatingHandler` (the NBP README shows one).
* **Cross-cutting HTTP concerns** (headers, auth, proxies, retries, path rewriting) are done with `DelegatingHandler`s / `HttpClient` configuration, not with library-specific hooks. Auth that needs protocol calls (a session login such as GUS `Zaloguj` returning a session id, token acquisition) or credentials passed as API arguments belongs in the provider and is designed in its provider ADR ([ADR-0016](0016-next-providers-and-package-naming.md)); whether a static API key is a handler or a provider option is decided there too.
* Separate clients per API area (currency, tables, gold) remain; a facade may be added later without breaking changes.
* **Namespace layout of a provider:** every public type of a provider package is in its root namespace, which equals the package id (`OpenUrzednik.Nbp`, `OpenUrzednik.MF.WhiteList`): the clients, their interfaces, the options, telemetry, log events, errors, and the models and enums. Their files live in the project root, so the folder matches the namespace. A model's name carries its own context (`VatStatusResult`, `AccountCheckResult`, `GoldPrice`), since no namespace adds it. Internal code (mappers, DTOs, validators, request pipelines) may use internal namespaces and folders by area or role (`.Common`, `.Dto`, `.Search`). A variant package ([ADR-0016](0016-next-providers-and-package-naming.md)) follows the same rule in its own root namespace (`OpenUrzednik.MF.WhiteList.FlatFile`), and the models it shares with its base package are in the base package's root.
* **NBP until 2.0:** `OpenUrzednik.Nbp` shipped 1.x with its models in `.Currency`, `.Table` and `.Gold`. A type's namespace is part of its identity and a move can't be forwarded or marked `[Obsolete]`, so NBP keeps that layout until 2.0, which moves the models into `OpenUrzednik.Nbp` ([#351](https://github.com/milten89/OpenUrzednik.NET/issues/351)).
* **Core and the integration packages:** in Core, the result types and their extensions on `Task<…>` (`EnsureSuccessAsync`, `MapAsync`, `BindAsync`) are in `OpenUrzednik.Core`, and operations on a result are instance methods (`EnsureSuccess`, `And`, `Map`, `Bind`, `Match`); errors, exceptions, telemetry and validation have their own namespaces (`.Errors`, `.Exceptions`, `.Telemetry`, `.Validation`), with extension methods next to the type they extend (`RecordError` in `.Telemetry`). Core isn't a provider and keeps these namespaces. No public namespace of Core or a provider is named after a kind of member (`.Extensions`, `.Options`, `.Models`); package names modelled on `Microsoft.Extensions.*` (`OpenUrzednik.Extensions.Logging`) and internal namespaces are exempt. DI registration stays in `Microsoft.Extensions.DependencyInjection`, as is usual for `IServiceCollection` extensions.
* **Constructors:** each client has two public constructors, one with only the `HttpClient` and one with the optional options, `TimeProvider` and `OpenUrzednikTelemetry` ([ADR-0025](0025-telemetry-abstractions-with-metrics.md)). Until 2.0, the released NBP clients instead have the prefix overloads of ADR-0025 without default values, next to their `[Obsolete]` 1.0 constructor. DI packages create the clients through the constructor with the options in a factory; a bare `AddHttpClient<TClient>()` isn't supported, because the typed-client activator finds no constructor it can use.

### Consequences

* Good, because the first-use experience is one line, with one `using`.
* Good, because unusual network setups are supported without forking.
* Good, because the public surface has no extension interfaces to keep stable.
* Good, because a new client that returns an existing model never forces a type to move.
* Bad, because a gateway that renames paths needs a handler that knows the NBP paths, and a change of those paths in a new version can break it.
* Bad, because the root namespace lists every public type, and two areas can't both have a type of the same name; model names must carry their context.
* Bad, because NBP follows the old layout until 2.0, so the two providers differ until then.

### Confirmation

The README shows the one-line default usage and the gateway handler, with only the provider's root namespace (for NBP, also its area namespaces until #351); tests construct clients with defaults and, through the internal constructor, with substituted URL builders. `tests/OpenUrzednik.Architecture.Tests` checks that every public type of a provider package is in the package's root namespace (NBP exempt until #351) and that no public namespace is named after a kind of member. The `reviewer` agent flags a public type or constructor parameter that shapes requests.

## Pros and Cons of the Options

### Simple constructors with optional, replaceable components

Public interfaces with public defaults, passed as optional constructor parameters (e.g. `INbpUrlBuilderFactory` for custom base URLs, paths or extra query parameters).

* Good, because a gateway can change base URLs, paths or query parameters without a handler.
* Bad, because every such interface is a long-term compatibility commitment, and a `DelegatingHandler` plus `NbpOptions.ApiUrl` covers the same gateway cases.

### Single facade client

* Good, because one object reaches every area.
* Bad, because an app that needs one area carries all of them, and the facade can be added later without a break; separate clients come first.

### A required URL-builder factory parameter

* Bad, because an implementation detail (URL building) is a required constructor argument, so the default case needs knowledge of internals.

### Models and enums in area namespaces (ADR-0007)

* Good, because types are grouped by API area, and two areas could reuse a type name.
* Bad, because a type that several areas use stays in one of them, so callers of the other areas import a namespace for it (`VatStatus` for the White List status client).
* Bad, because the areas are small (one to seven types; three areas held a single type), so the grouping adds `using`s more than it orders anything.
* Bad, because the [Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/names-of-namespaces) say not to give a namespace and a type in it the same name, and area names such as `Status` or `Check` invite that.

OpenAI's and Anthropic's .NET SDKs use this layout, with large areas that each have their own client; a type several areas use stays in its defining area there too.

### Every public type in the provider's root namespace

* Good, because one `using` reaches everything, and no type is ever placed by which areas share it.
* Good, because it is what the [Azure SDK .NET guidelines](https://azure.github.io/azure-sdk/dotnet_introduction.html) ask of libraries with few models ("these should keep the model types in the main namespace", with `Azure.Data.AppConfiguration` as the example), and what Octokit, Stripe's core types and the MongoDB driver do.
* Bad, because the root lists every type and names can't repeat across areas.

### The root namespace plus one `.Models` namespace

* Good, because the root shows only what an app uses to call the provider.
* Bad, because every caller needs two `using`s, and the Azure guidelines suggest it only when the number of models "is or might become large" (AWS's `.Model` per service, `Azure.Storage.Blobs.Models`, `Microsoft.Graph.Models`).
* Bad, because the namespace is named after a kind of type, which the rule above avoids for `.Extensions` and `.Options`.

### A shared namespace for the types several areas use, plus area namespaces

* Good, because each area keeps its own namespace and shared types have a neutral home.
* Bad, because callers need a shared and an area `using`, and each new client raises the question whether a type is shared yet; moving it later is a breaking change.
* Bad, because it pays off only with many areas or packages sharing types (Twilio's `Twilio.Types`, Azure Storage across packages, the Ministry of Finance's KSeF client), not with about ten models.

## More Information

* Supersedes [ADR-0007](0007-client-api-and-extensibility.md), released in v1.0.0; its history is in its own More Information.
* The survey behind the namespace options (2026-11-21, read from each project's source): Azure.Data.AppConfiguration, Azure.Security.KeyVault.Secrets, Azure.AI.TextAnalytics and Azure.Storage.Blobs; the AWS SDK; Google Cloud .NET; Microsoft Graph; Stripe.net; Octokit; Twilio; SendGrid; Elastic.Clients.Elasticsearch; the MongoDB driver; OpenAI; Anthropic; the Ministry of Finance's KSeF C# client; CompaniesHouse.NET.
* Implemented for the White List provider in [#350](https://github.com/milten89/OpenUrzednik.NET/issues/350), before its first release; NBP follows in 2.0 ([#351](https://github.com/milten89/OpenUrzednik.NET/issues/351)).
