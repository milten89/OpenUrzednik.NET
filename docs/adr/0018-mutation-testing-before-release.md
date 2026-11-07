---
status: accepted
date: 2026-11-07
confidence: medium
decision-makers: milten89
---

# 0018. Mutation testing as a required check before every release

## Context and Problem Statement

Line coverage doesn't show whether tests check behaviour. During #176, #225 and #217 the only proof that the privacy and validator tests catch a regression were mutations made by hand: drop the 400 server-message rule, log the real URI, skip the settlement check digit, and see a test fail. The library promises an error contract (ADR-0002), no identifiers in telemetry (ADR-0003, ADR-0006, ADR-0017) and validators that reject what the APIs reject; a test suite that passes with that code broken doesn't protect those promises. How do we measure, and keep, the tests' power to catch such changes, before a release reaches users?

## Decision Drivers

* Every release (and hotfix, and backport to a support branch) is checked, without relying on someone remembering to run it.
* An occasional check on `develop` catches erosion between releases.
* The scope follows risk to users, not today's test strength: all production code, minus mutants that can't harm anyone.
* Fast enough for a release PR, without slowing down every PR into `develop`.
* No change to the published packages.

## Considered Options

* Stryker.NET on every release PR as a required check, plus a weekly run
* Stryker.NET as a report only (weekly or on demand), no gate
* Stryker.NET on every PR into `develop` (changed files only)
* No mutation testing; keep hand-made mutation checks in PRs

## Decision Outcome

Chosen option: "Stryker.NET on every release PR as a required check, plus a weekly run", because it checks every release automatically and keeps PR CI into `develop` fast.

* **Tool:** [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) as a local tool (`.config/dotnet-tools.json`), with the Microsoft Testing Platform runner (`test-runner: mtp`): its VSTest runner can't run xUnit v3 tests. One target framework per run, `net10.0`; netstandard2.0-only code (`#if !NET`, polyfills) isn't mutated. `concurrency: 1` while the MTP runner under-reports kills in parallel sessions (stryker-net #3832).
* **Shards:** `tests/Mutation/shards.json` lists one shard per project under test, each with its Stryker config (`tests/Mutation/stryker-<shard>.json`). The workflow runs them on parallel runners. Shared source that only a test project compiles (`src/Identifiers`, `src/Privacy`, `src/Http/DependencyInjection`) is compiled into test-only host libraries in `tests/`, because Stryker never mutates a test project's own files. A new provider adds its shards (`/new-provider`).
* **Scope:** all production code on `net10.0`, except low-value mutants: log calls (message text and properties aren't contract, ADR-0003 and ADR-0011), mutants inside exception constructor calls (`*Exception.ctor`; a rule that matters, such as which constructor gets the inner exception, goes in a statement outside the call), `Polyfills/` and `Dto/`, and mutants marked `// Stryker disable once <Mutator> : <reason>` because they're equivalent (performance-only paths, caches), or because Stryker can't activate them (static field initializers, verified by hand). The comment covers every mutant of the listed kinds in the statement that follows, so it goes on the narrowest statement, and it never stands for missing tests.
* **Gate:** `tests/Mutation/run.py` fails a shard when no mutant was tested, when a mutant in one of its **strict** files is undetected (Survived or NoCoverage), or when its score is below its `break` value. Strict files are the code whose broken behaviour would harm users: the error contract and the HTTP executor, validators and options validation, results, errors and exceptions, identifiers and masking, mappers, models, payload selection and URL building, display paths and their logging, resilience translation. The rest is **ratchet** code: its `break` is the floor of its last score, raised whenever tests improve, until it can turn strict.
* **When it runs:** on every PR into `main` and `release/*.x` (release, hotfix and backport PRs), where the `mutation` job is a required status check (ruleset `release-mutation`, [GITHUB-SETUP](../GITHUB-SETUP.md)); every Monday on `develop`; on demand; and on a PR into `develop` that changes the mutation tooling. Not on other PRs into `develop`.
* Locally: `dotnet tool restore`, then `uv run tests/Mutation/run.py <shard>` (or `--all`).

### Consequences

* Good, because no release goes out with strict code whose tests don't catch a change in it.
* Good, because the weekly run shows erosion between releases, and the reports name the exact mutant.
* Good, because the shared source is now compiled into real libraries for the tests, which also runs it on net472 like production.
* Bad, because the MTP runner is a preview: a Stryker bug can fail a release PR; rerun the shard, and if it persists, fix or pin the tool before releasing.
* Bad, because single-threaded runs make a release PR wait several minutes longer; parallel runners keep it short.
* Bad, because new code in strict files needs tests that kill its mutants before the next release, which takes more work than coverage alone.

### Confirmation

* The required `mutation` check on release, hotfix and backport PRs; the weekly run's result.
* `/prepare-release` waits for `mutation` on the release PR and stops when it fails.
* The `reviewer` agent flags a `// Stryker disable` comment without a reason, or one that hides missing tests, and new strict code without tests that kill its mutants.

## Pros and Cons of the Options

### Stryker.NET on every release PR as a required check, plus a weekly run

* Good, because every release is checked automatically, and `develop` PRs stay fast.
* Bad, because a problem found on the release PR delays that release.

### Stryker.NET as a report only

* Good, because nothing can block.
* Bad, because nothing makes anyone read it before a release.

### Stryker.NET on every PR into `develop`

* Good, because problems show up in the PR that causes them.
* Bad, because every PR waits, and the preview runner's flaky results would hit every contributor. A possible addition once the MTP runner is stable.

### No mutation testing

* Bad, because hand-made checks cover only what the author thought of.

## More Information

* Issue [#227](https://github.com/milten89/OpenUrzednik.NET/issues/227). Revisit `concurrency` when stryker-net #3897 ships, and PR runs when the MTP runner leaves preview.
