---
status: accepted
date: 2026-11-06
confidence: medium
decision-makers: milten89
---

# 0016. Next providers and package naming

## Context and Problem Statement

[ADR-0010](0010-provider-readiness-gate.md) held new providers back until the first stable release of `OpenUrzednik.Nbp`. That release, 1.0.0, came out on 2026-11-06 with no open Urgent or High bug, so the gate is open. ADR-0010 left the choice of the next providers to a separate ADR and the package names to each provider ADR. Three providers are planned to run in parallel, so they need one naming scheme first. Which providers come next, how are their packages, namespaces and telemetry names built, and what happens to the empty `Gus`, `Krs` and `Mf` skeletons?

This ADR supersedes ADR-0010 and restates what stays in force from it.

## Decision Drivers

* A package name is fixed at its first release: renaming it changes the `ActivitySource` name, which is part of the telemetry contract ([ADR-0025](0025-telemetry-abstractions-with-metrics.md)).
* An app should get only the code and dependencies of the API it uses ([ADR-0024](0024-dependency-policy-with-a-7z-reader.md)).
* Polish institutions run several unrelated APIs (the Ministry of Finance runs the VAT White List next to systems with their own auth and protocols).
* The documentation and the public API are in English ([ADR-0009](0009-documentation-language.md)).
* Some public registers restrict automated access by law, which a client library must not ignore.

## Considered Options

* One package per API, English names, an institution prefix where the institution has or may get several APIs
* One package per institution (e.g. `OpenUrzednik.MF` for every Ministry of Finance API)
* One package per API with Polish names (e.g. `OpenUrzednik.MF.BialaLista`, `OpenUrzednik.Gus.Bir`)
* One package per API, always with an institution prefix (e.g. `OpenUrzednik.MS.Krs`)
* Names decided in each provider ADR (ADR-0010's rule)

## Decision Outcome

Chosen option: "One package per API, English names, an institution prefix where the institution has or may get several APIs", because it keeps each package's dependencies to its own API, gives names that read naturally next to the English API and docs, and fixes the names before any provider is released.

**The gate and the reference provider** (from ADR-0010, still in force):

* A new provider starts after the first stable release of `OpenUrzednik.Nbp` with no open Urgent or High bug (an open issue labelled `bug` with Priority Urgent or High in the [project](https://github.com/users/milten89/projects/2)). The gate opened on 2026-11-06 with 1.0.0.
* NBP is the reference implementation. New providers copy its structure, conventions and test approach.
* A provider project isn't published until it contains a usable client: it stays `IsPackable=false` until then, because every merge into `develop` that changes a package input publishes previews of all packable projects ([ADR-0008](0008-branching-versioning-and-release.md)).

**The next providers**, worked on in parallel (which of them ships in a given release is decided by that release's milestone, not here):

* the Ministry of Finance **VAT White List** HTTP API (Wykaz podatników VAT);
* the **KRS** open API of the Ministry of Justice (the National Court Register), with a legal check before any implementation (see the checklist);
* the **GUS BIR** service (the REGON register of Statistics Poland).

Not chosen now: the White List flat file, which needs a library to read 7z archives (SharpCompress, decided in [ADR-0024](0024-dependency-policy-with-a-7z-reader.md)), and any other API.

**Every provider ADR answers this readiness checklist:**

* protocol and HTTP methods;
* authentication and sessions;
* quota type (throttling, or a daily or per-IP quota) and retry advice;
* personal data in requests and responses, and its masking ([ADR-0025](0025-telemetry-abstractions-with-metrics.md), [ADR-0019](0019-shared-http-layer-tested-through-test-host.md));
* fixtures, and whether a test environment exists;
* terms of use and attribution;
* **the legal basis for automated access** (new: a register may require a permit or forbid unauthorised network access);
* the legal meaning of outputs;
* partial results of batch calls;
* extra dependencies ([ADR-0024](0024-dependency-policy-with-a-7z-reader.md));
* the package and namespace name, by the rules below.

**Package and namespace names:**

* One package per API. Never one package per endpoint, and never one per institution.
* `OpenUrzednik.<Institution>.<Api>` when the institution has or may get several APIs: `OpenUrzednik.MF.WhiteList`, `OpenUrzednik.Gus.Regon`. `OpenUrzednik.<Register>` when users know the register rather than the institution behind it: `OpenUrzednik.Krs`.
* A variant of an API with a different transport, other dependencies or a different way of use gets its own package, `<Package>.<Variant>`, which depends on the base package; the base package holds the models both share (e.g. `OpenUrzednik.MF.WhiteList.FlatFile`). More endpoints of the same API on the same transport (e.g. the White List's status-only WL2 endpoints) stay in the base package, as another client if needed.
* Names are English. .NET casing applies: two-letter acronyms in capitals (`MF`), longer ones in Pascal case (`Gus`, `Krs`).
* The root namespace equals the package id, and every public type of the package is in it, models included ([ADR-0026](0026-client-api-with-one-public-namespace-per-provider.md)).
* The nuget.org ids of the deprecated skeleton previews: `OpenUrzednik.Gus` and `OpenUrzednik.Mf` are not reused. `OpenUrzednik.Krs` is reused: its first real version must sort above `1.0.13-preview.gde7b6f8597`, the highest deprecated preview, which every lockstep version from `1.1.0-preview` on does ([ADR-0008](0008-branching-versioning-and-release.md)). A new provider is a feature, so it ships in a minor release.

**Telemetry names per package** (applying [ADR-0025](0025-telemetry-abstractions-with-metrics.md)):

* The `ActivitySource` name is the package id (e.g. `OpenUrzednik.MF.WhiteList`), so `AddSource("OpenUrzednik.*")` still enables every provider.
* The provider short name, used as the span-name prefix, in tags and as the `{provider}` log property, is the package id without `OpenUrzednik.`, in snake_case, with `_` between its segments: `mf_white_list`, `gus_regon`, `krs` (spans such as `mf_white_list.search.nip`).
* Each provider package lists its own log events (2xxx) and the shared HTTP ones in its `<Prefix>LogEvents` class.

**Type names** (clients, options, `<Prefix>Telemetry`, `<Prefix>LogEvents`): each provider ADR chooses its type prefix (e.g. `WhiteList` or `MFWhiteList`), before the first release; the namespace already carries the package id.

**Dependency injection:** at most one `<Package>.DependencyInjection` package per package with typed `HttpClient` clients. Its registration methods carry the package name (e.g. `AddOpenUrzednikMFWhiteList()`), so two DI packages never define the same method ([ADR-0024](0024-dependency-policy-with-a-7z-reader.md)).

**Quotas:** a quota or throttle that tells the caller to try again later is a `RateLimitExceededError`, with the retry time in its metadata when the API gives one or it can be derived (e.g. a block until midnight). A different condition gets a new condition type ([ADR-0021](0021-transport-subclasses-of-condition-errors.md)) only when a provider ADR shows one from the API's real responses.

**The skeletons:** the empty `src/OpenUrzednik.Gus`, `src/OpenUrzednik.Krs` and `src/OpenUrzednik.Mf` projects are removed; each provider adds its own project under its chosen name. Their empty previews on nuget.org stay deprecated and unlisted.

### Consequences

* Good, because an app that uses one API gets one package and only its dependencies; a heavy dependency (such as a 7z reader) stays in the variant that needs it.
* Good, because the names, source names and span prefixes are fixed before any provider ships, and are the same pattern for every provider.
* Good, because `AddSource("OpenUrzednik.*")` keeps working for every new package.
* Bad, because there are more packages to publish, document and keep AOT-safe (each needs a README, `README.pl.md`, an icon, an AotSmoke entry and package validation).
* Bad, because an institution prefix is a judgement call (will the institution get more APIs?), and the name can't change after the first release.
* Bad, because KRS can't start implementation until its legal status is clear.

### Confirmation

* The `reviewer` agent checks a new package's id, root namespace, `ActivitySource` name, short name and DI method names against this ADR.
* A provider ADR is rejected if it skips an item of the readiness checklist.
* The skeleton removal is a follow-up issue.

## Pros and Cons of the Options

### One package per institution

* Good, because there are few packages, and one install covers an institution.
* Bad, because every app would get every API's code and dependencies (the Ministry of Finance alone has unrelated systems with different auth and protocols).
* Bad, because one API's breaking change or heavy dependency affects users of the others.

### One package per API with Polish names

* Good, because the names match what Polish users search for (Biała Lista, BIR).
* Bad, because they mix with English types, members and docs ([ADR-0009](0009-documentation-language.md)), and the diacritics are dropped anyway.

### One package per API, always with an institution prefix

* Good, because every name follows one pattern.
* Bad, because some prefixes are unfamiliar: users know the KRS, not the Ministry of Justice (`OpenUrzednik.MS.Krs`).

### Names decided in each provider ADR

* Good, because each provider decides with full knowledge of its API.
* Bad, because providers worked on in parallel could choose inconsistent patterns, and a mismatch can't be fixed after the first release.

## More Information

* Supersedes [ADR-0010](0010-provider-readiness-gate.md) (decided in [#215](https://github.com/milten89/OpenUrzednik.NET/issues/215), 2026-11-06).
* Facts checked against official sources and read-only calls on 2026-11-06, for the provider ADRs to build on:
  * VAT White List ([API page](https://www.gov.pl/web/kas/api-wykazu-podatnikow-vat), [flat file](https://www.gov.pl/web/kas/plik-plaski)): no authentication, GET only; production `https://wykazpodatnikowvat.mf.gov.pl/api/`, test `https://wykazpodatnikowvat-test.mf.gov.pl/api/` (the former `wl-api.mf.gov.pl` and `wl-test.mf.gov.pl` are supported until 2026-12-01). NIP, REGON and account numbers are in the URL path. The `search` endpoints allow 100 requests a day (up to 30 entities each), after which access may be blocked until midnight. The `requestId` in an answer can prove a check to the tax authorities. The flat file is JSON in a 7z archive, published daily. Not verified: the HTTP status and body of a blocked request.
  * KRS (a live call; the legal change from a secondary source, not the act itself): `GET https://api-krs.ms.gov.pl/api/krs/OdpisAktualny/{krs}?rejestr=P&format=json`, no authentication, JSON; names and PESEL numbers in the answer are masked. The 2025 amendment of the KRS Act (Dz.U. 2025 poz. 1556) introduces access by administrative decision and penalties for unauthorised network access; how it applies to the open API is not verified.
  * GUS BIR ([API REGON](https://api.stat.gov.pl/Home/RegonApi) and its technical manual): SOAP 1.2 with WS-Addressing; `Zaloguj` with a user key returns a session id sent in the `sid` HTTP header (valid 60 minutes); a test environment with a public test key; batch searches of up to 20 identifiers; results are XML strings inside the SOAP answer; per-hour, per-minute and per-second limits; production keys are free, by e-mail to GUS.
* Follow-ups: the provider ADR issues [#212](https://github.com/milten89/OpenUrzednik.NET/issues/212) (White List), [#213](https://github.com/milten89/OpenUrzednik.NET/issues/213) (KRS) and [#214](https://github.com/milten89/OpenUrzednik.NET/issues/214) (GUS BIR); the skeleton removal ([#219](https://github.com/milten89/OpenUrzednik.NET/issues/219)) and the White List flat file ([#220](https://github.com/milten89/OpenUrzednik.NET/issues/220)).
* Until a release ships it, this accepted record is corrected in place, with a dated line under `Corrections before release`; a released record isn't edited ([ADR-0022](0022-record-architecture-decisions-frozen-at-release.md)): a changed decision gets a new record that supersedes this one.

### Corrections before release

* 2026-11-20 · [#220](https://github.com/milten89/OpenUrzednik.NET/issues/220) · The flat file's 7z reader is decided: SharpCompress, in [ADR-0024](0024-dependency-policy-with-a-7z-reader.md), which supersedes ADR-0004; the last More Information line now states ADR-0022's lifecycle instead of ADR-0001's. Reason: #220 chose the reader.
* 2026-11-21 · [#350](https://github.com/milten89/OpenUrzednik.NET/issues/350) · Every public type of a package goes in its root namespace instead of models in area namespaces below it: [ADR-0026](0026-client-api-with-one-public-namespace-per-provider.md) supersedes ADR-0007. Reason: the maintainer's decision after a survey of .NET SDKs.
* 2026-11-22 · [#366](https://github.com/milten89/OpenUrzednik.NET/issues/366) · References to superseded records point to their successors (ADR-0003 → ADR-0025, ADR-0004 → ADR-0024, ADR-0006 → ADR-0019, ADR-0012 → ADR-0021). Reason: the pre-1.1.0 review; the record freezes at the release cut.
