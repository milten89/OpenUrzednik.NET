Closes #<issue>

## What and why

<!-- What does this PR change, and why? -->

## Checklist

- [ ] Targets `develop` (or is a `release/X.Y` → `main` release PR, a `hotfix/*` → `main` PR, or a `merge/vX.Y.Z` → `develop` back-merge)
- [ ] `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass locally
- [ ] Tests cover new or changed behaviour, including error paths (unit + WireMock with real API payloads)
- [ ] Public API has XML docs; breaking changes are called out below
- [ ] Complies with the [ADRs](../docs/adr/README.md), or adds a new ADR for a changed decision
- [ ] If an informational part of a README changed (description, installation, quick start, package table, section names), `README.pl.md` too (ADR-0009)
- [ ] At least one release label set (`.github/release.yml`; the `release-label` check requires it, and the maintainer sets it if you can't)
- [ ] A release or hotfix PR into `main` adds `docs/release-notes/v<version>.md` (`/release-notes`)
- [ ] CI job names unchanged, or the ruleset update is described below

## Breaking changes / notes for reviewers

<!-- None, or describe. -->
