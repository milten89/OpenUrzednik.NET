---
status: accepted
date: 2026-11-08
confidence: medium
decision-makers: milten89
---

# 0020. Mutation testing as a required check before every release, on every target branch

## Context and Problem Statement

This record supersedes [ADR-0018](0018-mutation-testing-before-release.md). It keeps its decision (Stryker.NET as a required check before every release, a weekly run, strict and ratchet shards) and adds one rule: code that only some targets compile is mutated too.

Line coverage doesn't show whether tests check behaviour, so ADR-0018 introduced mutation testing for the code whose broken behaviour would harm users. Stryker.NET mutates one target framework per run, `net10.0`, and it can't run our net472 tests: its Microsoft Testing Platform runner fails on .NET Framework test projects (stryker-net #3421, fix in PR #3854, not released), and its VSTest runner never activates mutants with xUnit v3 tests, on net10.0 and on net472 alike (stryker-net #3920, #3117; checked in #251). So the netstandard2.0 branches (`#if NET` … `#else`) were never mutated, and three of them are separate copies of strict, privacy-relevant logic: the masking loop of `SensitiveValue.Mask`, the normalising loop of `IdentifierNormalizer` and the digit check of `IdentifierValidator`. The net472 test jobs run that code, but nothing showed that the tests would catch a change in it. How do we mutate every target's branch with the tools that work today?

## Decision Drivers

* Every release (and hotfix, and backport to a support branch) is checked, without relying on someone remembering to run it.
* An occasional check on `develop` catches erosion between releases.
* The scope follows risk to users, not today's test strength: all production code, on every branch a target compiles, minus mutants that can't harm anyone.
* Fast enough for a release PR, without slowing down every PR into `develop`.
* No change to the published packages.

## Considered Options

For the gate (unchanged from ADR-0018):

* Stryker.NET on every release PR as a required check, plus a weekly run
* Stryker.NET as a report only (weekly or on demand), no gate
* Stryker.NET on every PR into `develop` (changed files only)
* No mutation testing; keep hand-made mutation checks in PRs

For the branches only netstandard2.0 compiles:

* Test-only hosts that compile the code on net10.0 without the `NET` symbol
* A net472 shard on Stryker's VSTest runner
* Wait for a .NET Framework-capable MTP runner (stryker-net PR #3854)
* Don't mutate them (ADR-0018)

## Decision Outcome

Chosen options: "Stryker.NET on every release PR as a required check, plus a weekly run", because it checks every release automatically and keeps PR CI into `develop` fast; and "Test-only hosts that compile the code on net10.0 without the `NET` symbol", because it is the only way that works with Stryker.NET 5.0.0 and xUnit v3 today.

* **Tool:** [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) as a local tool (`.config/dotnet-tools.json`), with the Microsoft Testing Platform runner (`test-runner: mtp`): its VSTest runner can't run xUnit v3 tests. One target framework per run, `net10.0`. `concurrency: 1` while the MTP runner under-reports kills in parallel sessions (stryker-net #3832).
* **Shards:** `tests/Mutation/shards.json` lists one shard per project under test, each with its Stryker config (`tests/Mutation/stryker-<shard>.json`). The workflow runs them on parallel runners. Shared source that only a test project compiles (`src/Identifiers`, `src/Privacy`, `src/Http`) is compiled into test-only host libraries in `tests/`, because Stryker never mutates a test project's own files. A new provider adds its shards (`/new-provider`).
* **Every target branch:** code that only some targets compile (`#if NET` … `#else`) is mutated on each branch where a test-only host can compile and run it. For the netstandard2.0 branches, a host sets `DisableImplicitFrameworkDefines` (no `NET` symbol) and compiles the source on `net10.0`; a test project links the existing tests and sets the same property, so the tests' own `#if NET` expectations match. Its shard is strict like the default one, mutates the whole source again (built this way, not only the `#else` branches), and runs on the same CI runner. Providers still never mutate shared source ([ADR-0019](0019-shared-http-layer-tested-through-test-host.md) stands): the `-netstandard` host is a second build of it, not a provider's copy. Today: `identifiers-netstandard` (`tests/OpenUrzednik.Identifiers.NetStandardBranch.TestHost`) and `http-netstandard` (`tests/OpenUrzednik.Http.NetStandardBranch.TestHost`). Limits:
  * The branch is compiled against the net10.0 class library, so the shard checks its logic, not .NET Framework's runtime behaviour; the net472 test jobs still run the real thing. A branch that the net10.0 runtime never reaches gets a test that reaches it directly (`HttpContentJson`'s `null` content, which only .NET Framework produces), not a disable comment.
  * Code in a netstandard2.0 branch avoids the shapes whose mutants can't compile (pattern variables, declarations inside a condition), as the default branch does.
  * Polyfills that give netstandard2.0 a member .NET has are not reached: on net10.0 the real member wins. They stay excluded (`Polyfills/`) until a .NET Framework runner works.
  * A provider's own netstandard2.0 branches get a host when they produce mutants. NBP's don't today: a spike in #251 compiled the whole provider this way (with `DateOnly` as the `DateTime` alias) and found one mutant on a netstandard2.0-only line, in `DateOnlyPolyfills.DayNumber`, killed; not worth a shard.
* **Scope:** all production code, on every branch as above, except low-value mutants: log calls (message text and properties aren't contract, ADR-0003 and ADR-0011), mutants inside exception constructor calls (`*Exception.ctor`; a rule that matters, such as which constructor gets the inner exception, goes in a statement outside the call), `Polyfills/` and `Dto/`, and mutants marked `// Stryker disable once <Mutator> : <reason>` because they're equivalent (performance-only paths, caches), or because Stryker can't activate them (static field initializers, verified by hand). The comment covers every mutant of the listed kinds in the statement that follows, so it goes on the narrowest statement, and it never stands for missing tests.
* **Gate:** `tests/Mutation/run.py` fails a shard when no mutant was tested, when a mutant in one of its **strict** files is undetected (Survived or NoCoverage), or when its score is below its `break` value. Strict files are the code whose broken behaviour would harm users: the error contract and the HTTP executor, validators and options validation, results, errors and exceptions, identifiers and masking, mappers, models, payload selection and URL building, display paths and their logging, resilience translation. The rest is **ratchet** code: its `break` is the floor of its last score, raised whenever tests improve, until it can turn strict.
* **When it runs:** on every PR into `main` and `release/*.x` (release, hotfix and backport PRs), where the `mutation` job is a required status check (ruleset `release-mutation`, [GITHUB-SETUP](../GITHUB-SETUP.md)); every Monday on `develop`; on demand; and on a PR into `develop` that changes the mutation tooling. Not on other PRs into `develop`.
* Locally: `dotnet tool restore`, then `uv run tests/Mutation/run.py <shard>` (or `--all`).

### Consequences

* Good, because no release goes out with strict code whose tests don't catch a change in it, on either branch of the shared source.
* Good, because the weekly run shows erosion between releases, and the reports name the exact mutant.
* Good, because the shared source is compiled into real libraries for the tests, which also runs it on net472 like production.
* Good, because a separate copy of a rule for netstandard2.0 can no longer drift from its tests unnoticed.
* Bad, because the MTP runner is a preview: a Stryker bug can fail a release PR; rerun the shard, and if it persists, fix or pin the tool before releasing.
* Bad, because single-threaded runs make a release PR wait several minutes longer; parallel runners keep it short.
* Bad, because new code in strict files needs tests that kill its mutants before the next release, which takes more work than coverage alone.
* Bad, because each host and its test project are two more projects, and the linked tests run again in the normal test jobs (seconds).
* Bad, because the `identifiers` and `http` runners mutate their source twice: the `http` job, already among the longest (about 4 minutes), takes roughly twice as long. Limiting the `-netstandard` configs to the files with an `#if` would be faster, but a file that gains one would need a config change.
* Bad, because the netstandard2.0 branches are mutated on .NET's runtime, not .NET Framework's: a bug that only .NET Framework's behaviour shows is left to the net472 tests.

### Confirmation

* The required `mutation` check on release, hotfix and backport PRs; the weekly run's result.
* `/prepare-release` waits for `mutation` on the release PR and stops when it fails.
* The `reviewer` agent flags a `// Stryker disable` comment without a reason, or one that hides missing tests; new strict code without tests that kill its mutants; and a new `#if` branch in strict code that no shard mutates.

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

### Test-only hosts without the `NET` symbol

* Good, because it works today with the MTP runner, and the existing tests killed every mutant in the three copied rules.
* Bad, because the branch runs on .NET's runtime, and polyfills stay out of reach.

### A net472 shard on the VSTest runner

* Bad, because with xUnit v3 no mutant is activated: every mutant survives (stryker-net #3920; reproduced on net472 in #251).

### Wait for a .NET Framework-capable MTP runner

* Good, because it would mutate the real netstandard2.0 build, polyfills included.
* Bad, because PR #3854 is open with no release date; the gap would stay open until then.

### Don't mutate the netstandard2.0 branches

* Bad, because separate copies of masking and validation logic stay unchecked.

## More Information

* Issues [#227](https://github.com/milten89/OpenUrzednik.NET/issues/227) (ADR-0018) and [#251](https://github.com/milten89/OpenUrzednik.NET/issues/251). Revisit `concurrency` when stryker-net #3897 ships, PR runs when the MTP runner leaves preview, and a real net472 shard (polyfills included) when a release ships stryker-net PR #3854; `/stryker` tracks these.
