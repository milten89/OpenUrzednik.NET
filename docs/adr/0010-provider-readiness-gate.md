---
status: accepted
date: 2026-10-30
decision-makers: milten89
---

# 0010. Framework first: NBP as the reference provider

## Context and Problem Statement

The repository contains skeletons for GUS, KRS and MF, but the shared framework (error model, HTTP layer, telemetry, DI, multi-targeting) is still evolving and the NBP client had known bugs and design issues (as of 2026-10-01). Adding providers now would copy those issues into every package. When do we start new providers?

## Decision Outcome

* No work on new providers (including the existing GUS/KRS/MF skeletons) until the gate opens: the first stable release of `OpenUrzednik.Nbp`, with no open P0 or P1 bug. An open P0 or P1 bug is an open issue labelled `bug` with Priority P0 or P1 in the [project](https://github.com/users/milten89/projects/2).
* NBP is the reference implementation. New providers copy its structure, conventions and test approach.
* The choice and order of the next providers is decided afterwards, in a separate ADR.
* Every provider ADR answers this readiness checklist:
  * protocol and HTTP methods;
  * authentication and sessions;
  * quota type (throttling, or a daily or per-IP quota) and retry advice;
  * personal data in requests and responses, and its masking ([ADR-0003](0003-telemetry-abstractions.md), [ADR-0006](0006-shared-http-layer.md));
  * fixtures, and whether a test environment exists;
  * terms of use and attribution;
  * the legal meaning of outputs;
  * partial results of batch calls;
  * extra dependencies ([ADR-0004](0004-dependency-policy.md));
  * the package and namespace name.
* Package and namespace names use .NET casing: two-letter acronyms in capitals (`MF`, not `Mf`), longer ones in Pascal case. Whether a package covers one API (e.g. `OpenUrzednik.BialaLista`) or an institution and dataset (e.g. `OpenUrzednik.MF.WhiteList`) is decided in each provider ADR. Renaming a released package changes its `ActivitySource` name, which is part of the telemetry contract ([ADR-0003](0003-telemetry-abstractions.md)), so the name is decided before the first release. The `Gus`, `Krs` and `Mf` skeletons keep their names until their provider ADR.
* Skeleton packages are not published (`IsPackable=false`) until they contain a usable client. The empty previews of Gus, Krs and Mf published before that rule (up to `1.0.13-preview`) are deprecated and unlisted on nuget.org. A provider's first real version must sort above `1.0.13-preview.gde7b6f8597`, the highest of them. Lockstep versions from `1.1.0-preview` on do; `1.0.0` to `1.0.13` don't. A new provider is a feature, so it ships in a minor release, 1.1.0 or later ([ADR-0008](0008-branching-versioning-and-release.md)).

### Consequences

* Good, because framework problems are fixed once, before they are copied.
* Bad, because no new provider is available for a while.

### Confirmation

PRs touching `src/OpenUrzednik.Gus|Krs|Mf` are rejected unless they implement this ADR's follow-ups (e.g. `IsPackable=false`). Whether the gate is open is checked against nuget.org (a stable `OpenUrzednik.Nbp`) and the project (no open issue labelled `bug` with Priority P0 or P1).

## More Information

**2026-10-17 change: an objective gate** ([#75](https://github.com/milten89/OpenUrzednik.NET/issues/75)). The gate said "until `OpenUrzednik.Nbp` is complete", measured by the design issues in `docs/BACKLOG.md`. That file is now a closed record (work is tracked in issues), and its last open P0–P2 item was a documentation sentence, so the condition no longer said anything. The maintainer replaced it with an event anyone can check: the first stable release of `OpenUrzednik.Nbp`, with no open P0 or P1 bug. The skeletons' empty previews (52 versions each, published before `IsPackable=false`) were deprecated and unlisted, and the READMEs, the Core README and CONTRIBUTING now say the providers are planned, not started.

**2026-10-30 change: readiness checklist and naming** ([#109](https://github.com/milten89/OpenUrzednik.NET/issues/109)). The audit of the documentation found that the first real providers would hit questions no ADR answers. The checklist above makes each provider ADR answer them before any code. The MF White List shows what the checklist surfaces (to verify in its provider ADR): a daily quota that blocks the caller until midnight, not a throttle; `requestId` and `requestDateTime` in the answer as the proof of a check; partial results in batch calls; a flat file in 7z format; a test host (`wl-test.mf.gov.pl`). Each of these touches the error model, the executor, the fixtures or the dependencies. The naming rule was decided at the same time, because the skeleton `Mf` breaks the .NET acronym rule and a rename after release would change the `ActivitySource` name.
