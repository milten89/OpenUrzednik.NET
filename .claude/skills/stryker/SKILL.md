---
name: stryker
description: Run, read and fix Stryker.NET mutation tests in OpenUrzednik.NET - the shards and their gate (tests/Mutation, ADR-0018), scoped runs of one file, reading mutation-report.json, triaging survivors, NoCoverage and equivalent mutants, freeing compile-error collateral (mutants Stryker discards next to a CompileError), // Stryker disable comments, Stryker configs and shards, and known Stryker.NET 5 / MTP bugs. Use whenever a mutation shard fails, a report lists survivors or compile errors, code in a strict file changes, a Stryker config, shard or the mutation workflow is edited, or Stryker.NET's version changes and its known issues need updating.
argument-hint: "[shard | src file | path to mutation-report.json]"
---

# Mutation testing with Stryker.NET

[ADR-0018](../../../docs/adr/0018-mutation-testing-before-release.md) is binding; this skill is the working manual. Stryker.NET is a local tool pinned to **5.0.0** (`.config/dotnet-tools.json`, `rollForward: false`). Facts about Stryker below cite the 5.0.0 source (tag `dotnet-stryker@5.0.0`, commit `6e77a34`; short paths such as `Compiling/…` are under `src/Stryker.Core/Stryker.Core/`, other projects' paths start with the project folder), its docs, or an issue (`sn#N` = stryker-mutator/stryker-net#N). Known bugs and their workarounds: [references/known-issues.md](references/known-issues.md).

## 1. Run

```bash
dotnet tool restore                          # once per clone
uv run tests/Mutation/run.py --list          # shard names
uv run tests/Mutation/run.py nbp             # one shard (several: nbp core)
uv run tests/Mutation/run.py --all
```

| Shard | Workdir (project mutated) | Strict globs (`tests/Mutation/shards.json`) |
|---|---|---|
| `identifiers` | `tests/OpenUrzednik.Identifiers.TestHost` | `src/Identifiers/**`, `src/Privacy/**` |
| `nbp` | `src/OpenUrzednik.Nbp` | validation, URL builders, extensions, models, payload, pipeline, options, `NbpDateOnlyExtensions.cs` (11 globs) |
| `http` | `tests/OpenUrzednik.Http.TestHost` | `src/Http/Infrastructure/**` |
| `http-di` | `tests/OpenUrzednik.Http.DependencyInjection.TestHost` | `src/Http/DependencyInjection/**` |
| `core` | `src/OpenUrzednik.Core` | results, `Errors/`, `Exceptions/`, `Validation/` |
| `di`, `logging` | the package's `src/` folder | the whole package |
| `diagnostics` | `src/OpenUrzednik.Diagnostics` | none (ratchet only) |

- Output: `artifacts/mutation/<shard>/reports/mutation-report.{json,html,md}`, plus `artifacts/mutation/summary.md` (git-ignored). `run.py` deletes the old `reports/` first, so a failed run never leaves a stale report behind.
- Exit codes of `run.py`: 0 every shard passed, 1 a gate failed, 2 a shard couldn't run, has no report, tested no mutant, or has a strict glob matching no file.
- Time: on CI each shard job takes about 1–6 minutes including setup (`nbp` and `http` are the longest; runs 42355286460, 42403860292). Single-threaded (`concurrency: 1`, section 6), so local runs are of the same order.
- **Never run two Stryker runs, or Stryker and `dotnet build`/`test`, at the same time in one checkout.** They share `obj/` and `bin/`; corrupted `obj/` has been seen here. For parallel work use a separate `git worktree`. `run.py` also gives each shard its own `TEMP`, because the MTP runner passes the active mutant through a fixed file in the temp folder (`stryker-mutant-<id>.txt`, `Stryker.TestRunner.MicrosoftTestPlatform/MicrosoftTestingPlatformRunner.cs:78`).

### Scoped run (one file, fast loop)

Run Stryker directly from the shard's workdir with the shard's config and an output folder outside the repo (the scratchpad):

```bash
cd src/OpenUrzednik.Nbp
TEMP=<scratch>/tmp TMP=<scratch>/tmp TMPDIR=<scratch>/tmp dotnet stryker --config-file ../../tests/Mutation/stryker-nbp.json \
  -m "**/NbpOptionsValidator.cs" --output <scratch>/nbp-options -r json --verbosity warning --skip-version-check
uv run ../../.claude/skills/stryker/references/mutants.py list <scratch>/nbp-options/reports/mutation-report.json
```

One file of the core shard took 41 s this way (2026-11-07). Stryker still mutates and compiles the whole project and filters by `-m` afterwards: the other files' mutants show as `Ignored` (`Removed by mutate filter`), and compile-error collateral is the same as in a full run.

- A CLI option **replaces** the config's value: the config file is read first, then the command line (`Stryker.CLI/Stryker.CLI/ConfigBuilder.cs:66-71`), and a repeated option such as `-m` or `-r` sets the whole list (`Stryker.CLI/Stryker.CLI/CommandLineConfig/CommandLineConfigReader.cs:175`). With `-m` the config's exclusions (`!**/Polyfills/**`, `!**/Dto/**`, `!**/Http/**`) are gone, so keep the pattern narrow or repeat them. The same holds for `-r`: once you pass one, pass `-r json` too, or the JSON report the scripts read isn't written.
- A scoped run is for iterating. The gate is `run.py` on the whole shard: run it before the PR when strict code changed (CLAUDE.md, Testing conventions).
- Agent-friendly flags, adapted from Stryker's own `run-stryker` skill (see the end): `--verbosity warning` (also hides the banner), `--skip-version-check`, `-r json`. Its other suggestion, `-r cleartext`, prints a row for every file of the project, excluded ones included: skip it and read the JSON.

### Seeing what Stryker did

| Need | Flag | What you get |
|---|---|---|
| Which mutants were rolled back and why | default `info` | `Safe Mode! Stryker will remove all mutations in <method>…` and `An unidentified mutation … resulted in a compile error` warnings (`Compiling/CSharpRollbackProcess.cs:232-240`) |
| Each rollback step | `--verbosity debug` | `RollBacking mutations from <file>`, `Found mutant <id> of type '<Mutator>'`, `Trying compilation for the N time` |
| The injected code | `--diag --verbosity trace` | the full mutated syntax tree per file (`MutationTest/CsharpMutationProcess.cs:50-53`) and compiler errors with source lines (`Dumping build error in file`). The messages come in the OS language without the `CS` id (Polish on this machine). The docs call this flag `--dev-mode`; the 5.0.0 CLI flag is `--diag` (`CommandLineConfigReader.cs:213`, sn#3977). |
| A log file | `-L` / `--log-to-file` | `<output>/logs/log-<date>.txt` |

## 2. The gate

`run.py` reads each shard's JSON report and fails it when Stryker exits non-zero, when **no mutant was tested** (Stryker exits 0 when it can't compute a score, PR sn#3672), when a strict glob matches no file, when a mutant in a strict file is **Survived or NoCoverage**, or when the score is below the shard's `break`. Score = (Killed + Timeout) / (Killed + Timeout + Survived + NoCoverage); CompileError, RuntimeError and Ignored don't count (`Mutants/Mutant.cs:26`). Today every shard has `break: 100`.

- **Strict** files: code whose broken behaviour would harm users (ADR-0018 lists them). No undetected mutant may stay. **Ratchet** code: its `break` is the floor of its last score, raised when tests improve, until it turns strict.
- CI (`.github/workflows/mutation.yml`): required check `mutation` on PRs into `main` and `release/*.x`; Monday 02:00 UTC on `develop`; `workflow_dispatch`. `mutation-tooling.yml` calls it on PRs into `develop` that touch `tests/Mutation/**`, the test hosts, the workflows or `.config/dotnet-tools.json`. A PR into `develop` that changes only `src/` doesn't run it: run the shards locally, and if CI evidence is wanted, dispatch it on the branch (`gh workflow run mutation.yml --ref <branch>`; not read-only, so ask first). Reports are the `mutation-<runner>` artifacts (30 days).
- One shard per project under test, one CI runner per shard (`adapters` runs `di logging diagnostics`). Stryker never mutates a test project's own files, so shared source that only tests compile lives in test-only host libraries (`tests/OpenUrzednik.Identifiers.TestHost`, `…Http.TestHost`, `…Http.DependencyInjection.TestHost`) and is mutated once, in the host's shard; provider and DI shards exclude it (`!**/Http/**`). A new provider adds its shards (`/new-provider`).
- Every config (`tests/Mutation/stryker-<shard>.json`) shares: `test-runner: mtp`, `target-framework: net10.0`, `concurrency: 1`, `coverage-analysis: perTest`, `mutation-level: Standard`, `mutate` without `Polyfills/` and `Dto/`, `ignore-methods` `*Log`, `LogUnreadableBody`, `*Exception.ctor` (log calls and exception constructor arguments aren't contract, ADR-0018), reporters html, json, markdown, progress. Keep them aligned; only `project`, `test-projects` and extra exclusions differ.

## 3. Read the report

`mutation-report.json` follows the mutation-testing-report-schema (`schemaVersion` 2): `files.<path>.mutants[]` with `id`, `mutatorName`, `replacement`, `location`, `status`, `statusReason`, `coveredBy`, `killedBy` (test ids), and `testFiles.<path>.tests[]` mapping ids to test names. File keys are absolute paths (sn#3725); `mutants.py` makes them relative to the repository root (from the first `src/` or `tests/` segment), so reports from a worktree or a CI artifact compare.

| Status | Meaning | Gate |
|---|---|---|
| `Killed` | a test failed with the mutant active | detected |
| `Timeout` | the run exceeded its time limit (e.g. a removed `i++`) | detected |
| `Survived` | covering tests ran and passed | undetected: fails a strict file |
| `NoCoverage` | no test executes the mutant | undetected: fails a strict file |
| `CompileError` | never tested: `Mutant caused compile errors` (rolled back, section 5) or `Could not be injected in code.` | not counted, invisible to the gate |
| `RuntimeError` | the test run itself crashed | not counted |
| `Ignored` | filtered: `statusReason` says by what (`Removed by method filter`, `Removed by block already covered filter`, the reason of a `// Stryker disable` comment) | not counted |

Use the bundled script (stdlib only) instead of ad-hoc parsing:

```bash
S=.claude/skills/stryker/references/mutants.py
uv run $S list artifacts/mutation/nbp/reports/mutation-report.json                # non-killed, non-ignored, per file
uv run $S list <report> --file NbpOptions --tests --all                            # every mutant of a file, with killing/covering tests
uv run $S diff <before.json> <after.json> [--file X] [--ignore-lines]              # status changes by file, line, mutator, replacement
```

Keep the "before" report: copy `artifacts/mutation/<shard>/reports/mutation-report.json` to the scratchpad before rerunning, because `run.py` deletes it. Mutant ids change between runs; the diff matches mutants by content. The HTML report shows the same data with the source.

## 4. Triage each undetected mutant

Work file by file from `mutants.py list`. For each Survived or NoCoverage mutant (and each CompileError, section 5), decide:

1. **Missing test** (the usual case). Write a test that fails with the mutant: follow CLAUDE.md's testing conventions (one partial test class file per method, `Method_Condition_ExpectedResult`, deterministic data, `StubHttpMessageHandler`). Rerun a scoped run and check with `--tests` that the new test is in `killedBy`. NoCoverage means no test reaches the line at all; check first that the code is reachable on `net10.0`.
2. **Equivalent mutant** (no observable change, e.g. a performance-only path or a cache). Prefer **rewriting the code so the mutant doesn't exist**, at the same cost: PR #240 changed `end < 0` after `IndexOf` to `end == -1`, removing the equivalent `<= 0` mutant without hiding the killable `> 0`. Add a short comment citing ADR-0018 so the code doesn't drift back.
3. Only when no such rewrite exists: `// Stryker disable once <Mutator> : <reason>` on the line before the narrowest statement. Rules (ADR-0018, and the `reviewer` agent checks them):
   - Only for an equivalent mutant or one Stryker can't activate (item 5), never for a missing test.
   - It covers every mutant of the listed kinds in the **whole following statement** (a block statement included). After adding it, diff the reports: only the intended mutants may turn `Ignored`; a killed one turning `Ignored` means the comment hides a test.
   - Mutator names are the enum names (`Stryker.Abstractions/Mutator.cs`): `Statement`, `Arithmetic`, `Block`, `Equality`, `Boolean`, `Logical`, `Assignment`, `Unary`, `Update`, `Checked`, `Linq`, `String`, `Bitwise`, `Initializer`, `Regex`, `NullCoalescing`, `Math`, `StringMethod`, `Conditional`, `CollectionExpression`; several are comma-separated. Examples: `src/Http/Infrastructure/RestRequestExecutor.cs:486`, `src/OpenUrzednik.Nbp/Common/NbpConnection.cs:14`.
4. **Timeout** counts as detected, but the MTP runner produces false timeouts (sn#3956, sn#3915). A loop that no longer ends (a removed `i++`) is a real one; for any other Timeout in strict code, make the change by hand and check that a test fails, or add one.
5. **Static initializers**: the MTP runner reuses one test host across mutants, so a static field initializer's mutant is usually never activated and survives (sn#3897). Check by hand that breaking it fails tests, then disable it with that reason (`src/OpenUrzednik.Nbp/Common/NbpConnection.cs:14`).

Report the before/after counts per shard in the PR, as PRs #237, #240 and #247 do.

## 5. Compile-error collateral (the main pitfall)

Stryker puts all mutants into one compilation (`IsActive(n) ? mutated : original`). When that fails, `CSharpRollbackProcess` removes mutants until it compiles (up to 50 attempts, `Compiling/CsharpCompilingProcess.cs:38`) and marks every removed one `CompileError`:

- For each compiler error it takes the node at the error and walks **up** to the nearest enclosing mutant (`FindMutationWithNode`, `CSharpRollbackProcess.cs:93-112`). That mutant is blamed even if it compiles fine; the broken one may be a sibling or a child.
- CS0165/CS0177 (unassigned variable) first look for a mutant that erases the assignment (`ScanErasingMutation`); a negated pattern variable isn't recognised, so it falls through to the walk-up from the variable's use.
- If no mutant is found, **Safe Mode** rolls back every mutant in the enclosing method, accessor or constructor, or in the whole file (`ScanForSuspiciousMutations`, lines 210-249).

So one uncompilable mutant can silently remove compilable, killable neighbours from testing, and the gate can't see it (CompileError isn't counted). Shapes found here and their fixes (PRs #237, #247):

| Shape | Uncompilable mutant | Fix used |
|---|---|---|
| Pattern variable `x is { } y`, `value is int code` | Equality `is not` leaves `y` unassigned where it is used (CS0165); for `{ } y` the ternary also declares `y` twice (CS0128, sn#3597). The mutant around the use (`y + 1` → `y - 1`) is blamed. | `HasValue` + `GetValueOrDefault()` for nullable structs; a local + `is not null` for references |
| Declaration inside a condition (`is { } errors ? … : …`, `TryGet(out var x) ? …`) | Conditional `true`/`false`, Logical, Negate remove or re-scope the declaration | Declare the local (or call `TryGet…`) in its own statement first |
| `list.Count` property (`IReadOnlyList<T>`) | Linq `Count` → `Sum` on a property: a method group (CS0019/CS0173); the comparison around it is blamed | Read `Count` into a local (`var count = errors.Count;`); the local's own `Sum` mutant stays a harmless CompileError |
| `StringBuilder.Append(...)` | Linq `Append` → `Prepend` (no such method) | Leave it: it blames nothing else |
| `string.Create(provider, $"…")` | String `$""` as `IsActive(n) ? $"" : $"…"` loses the `ref` handler conversion (CS1620) | Leave it: genuine, hides nothing |

How to tell genuine from collateral: for each CompileError ask **"would this mutant compile on its own?"** If yes (`errors.Count != 1`, `!=` → `==`, a removed statement), it is collateral; find the uncompilable mutant at or inside the same expression or statement (`mutants.py list --file X --all`, then `--diag --verbosity trace` if unclear) and restructure that code. The remaining CompileErrors must be genuinely uncompilable and must not wrap anything; state that in the PR.

Configuration can't free collateral: `ignore-mutations` (even `["linq"]`) and `ignore-methods` filter **after** compilation and skip CompileError mutants (`MutationTest/CsharpMutationProcess.cs:118-126`, sn#962; verified on the core shard and a repro). Only a `// Stryker disable` comment acts before compilation: the mutant is created as Ignored (`Mutants/CsharpMutantOrchestrator.cs:170-176`) and only Pending mutants are injected (`Mutants/MutationStore.cs:267`), so its neighbours compile. Prefer the code rewrite. A comment is allowed only under ADR-0018's second reason, a mutant Stryker can't activate: its reason must say the mutant can't compile, and the report diff must show that it hides no mutant a test kills.

## 6. MTP and xUnit v3 specifics

- `test-runner: mtp` is required: the VSTest runner can't run xUnit v3 tests (ADR-0018). The MTP runner is a preview; a Stryker bug can fail a shard. Rerun it, and if it persists, fix or pin the tool before releasing.
- `concurrency: 1` until sn#3897 ships: in parallel sessions the MTP runner under-reports kills (sn#3832), turning killed mutants into survivors.
- One target framework per run (`net10.0`). Code that only netstandard2.0 compiles (`#if !NET`, `Polyfills/`) is never mutated; net8.0-only branches aren't either. Tests still run that code on net472 in the normal test jobs.
- Stryker 5 needs the .NET 10 runtime, and restore needs the newest SDK; CI restores the solution before running, because without restored packages Stryker's design-time build drops test projects silently (fewer tests, false NoCoverage).
- A run where nothing was tested exits 0 (PR sn#3672: with strong-named assemblies, a version that differs between Stryker's design-time build and the real build makes every mutant a RuntimeError). `run.py` treats it as an error. The assemblies here are strong-named and versioned by Nerdbank.GitVersioning, which needs full history during Stryker's build (`fetch-depth: 0` in `mutation.yml`): in a shallow clone, expect this failure. Check the summary, not only the exit code, when running Stryker by hand.
- Keep the test suite green: under MTP one failing test aborts the run (`Initial testrun has more than 50% failing tests`, sn#3952).
- Test-only hosts set `IsTestProject=false` and get no xUnit/MTP packages; a project referencing Microsoft.Testing.Platform is taken for a test project (sn#3887).

Every other known bug, with its state in 5.0.0 and the workaround used here: [references/known-issues.md](references/known-issues.md). Check it before blaming the code, and add to it when you find a new one.

## 7. Keep the known issues current

[references/known-issues.md](references/known-issues.md) is a snapshot (its "Reviewed" line has the date). Update it:

- **When Stryker.NET changes version** (`.config/dotnet-tools.json`, by hand or in a Dependabot PR): in that PR, before it is merged, because the workarounds may change with the version.
- **When a shard misbehaves** and the symptom isn't in the list: search upstream first (`gh issue list -R stryker-mutator/stryker-net --state all --search "<symptom>"`).
- **On request** ("update the Stryker known issues").

Steps:

1. Read the release notes of every Stryker.NET version since the one in the skill (`gh release list -R stryker-mutator/stryker-net`, `gh release view <tag> -R stryker-mutator/stryker-net`). For each row marked "fixed after 5.0.0" or "open", check whether the release fixes it; then re-check the workaround that depends on it. Known ones to revisit: `concurrency: 1` (sn#3832, PR #3897), `run.py`'s per-shard `TEMP` (PR #3724), the static-initializer disable comments (PR #3897), the `Count` local (sn#2762, PR #3601), false timeouts (PRs #3957, #3917). Change a workaround only with a run that shows it is no longer needed, and in its own PR (ADR-0018 governs the configs).
2. Review the issues opened or closed since the "Reviewed" date: `gh issue list -R stryker-mutator/stryker-net --state all --search "updated:>=YYYY-MM-DD" --limit 200 --json number,title,state,labels,closedAt`. Keep those that can affect this setup (the first paragraph of known-issues.md lists it); skip the excluded areas listed there.
3. For each kept issue, add or update a row: number and link, state (and the version that fixes it), the symptom as it shows in a report or log, whether this repository's Stryker version is affected, and the workaround used or recommended here. Mark how it was verified (**source**, **here**, **repro**, **unverified**); verify in the source of the pinned tag or with a small repro where it is cheap.
4. Update the version, tag and commit in this file's introduction and in known-issues.md, the "Reviewed" line (date, how many issues), and every section of this file whose facts changed (source line numbers move between versions).
5. A bug that affects this repository and isn't reported upstream (or was closed without its cost being understood): write a draft issue (title, minimal repro, expected vs actual, version and config) and give it to the maintainer. Never post upstream yourself; the maintainer decides.
6. Outside a version change, the update is its own PR (`skip-changelog`); commit and open it only as CLAUDE.md allows.

---

License: MIT ([LICENSE](LICENSE)); copy and adapt it freely. Option advice in section 1 is adapted from Stryker.NET's `run-stryker` agent skill (`.github/skills/run-stryker/SKILL.md`, stryker-mutator/stryker-net at `dotnet-stryker@5.0.0`, commit 6e77a3451bac4793e9c839c3ff4055c1b30ca3af), Apache License 2.0; see the notice in [LICENSE](LICENSE).
