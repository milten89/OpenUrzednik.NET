---
status: accepted
date: 2026-10-01
decision-makers: milten89
---

# 0010. Framework first: NBP as the reference provider

## Context and Problem Statement

The repository contains skeletons for GUS, KRS and MF, but the shared framework (error model, HTTP layer, telemetry, DI, multi-targeting) is still evolving and the NBP client has known bugs and design issues. Adding providers now would copy those issues into every package. When do we start new providers?

## Decision Outcome

* No work on new providers (including the existing GUS/KRS/MF skeletons) until `OpenUrzednik.Nbp` is complete: all known bugs fixed, design issues from `docs/BACKLOG.md` resolved, and the decisions in ADR-0002 – ADR-0007 implemented.
* NBP is the reference implementation. New providers copy its structure, conventions and test approach.
* The choice and order of the next providers is decided afterwards, in a separate ADR.
* Skeleton packages are not published (`IsPackable=false`) until they contain a usable client.

### Consequences

* Good, because framework problems are fixed once, before they are copied.
* Bad, because no new provider is available for a while.

### Confirmation

PRs touching `src/OpenUrzednik.Gus|Krs|Mf` are rejected unless they implement this ADR's follow-ups (e.g. `IsPackable=false`).
