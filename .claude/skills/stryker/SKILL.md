---
name: stryker
description: Run, read and fix Stryker.NET mutation tests in OpenUrzednik.NET - the shards and their gate (tests/Mutation, ADR-0020), scoped runs of one file, reading mutation-report.json, triaging survivors, NoCoverage and equivalent mutants, freeing compile-error collateral (mutants Stryker discards next to a CompileError), // Stryker disable comments, Stryker configs and shards, and known Stryker.NET 5 / MTP bugs. Use whenever a mutation shard fails, a report lists survivors or compile errors, code in a strict file changes, a Stryker config, shard or the mutation workflow is edited, or Stryker.NET's version changes and its known issues need updating.
argument-hint: "[shard | src file | path to mutation-report.json]"
---

# Mutation testing with Stryker.NET

[ADR-0020](../../../docs/adr/0020-mutation-testing-of-every-target-branch.md) is binding; this skill is the working manual. Stryker.NET is a local tool pinned to **5.0.0** (`.config/dotnet-tools.json`, `rollForward: false`). Facts about Stryker cite the 5.0.0 source (tag `dotnet-stryker@5.0.0`, commit `6e77a34`; short paths such as `Compiling/…` are under `src/Stryker.Core/Stryker.Core/`, other projects' paths start with the project folder), its docs, or an issue (`sn#N` = stryker-mutator/stryker-net#N).

Read a reference file only when its case comes up:

| File | When |
|---|---|
| [references/known-issues.md](references/known-issues.md) | a shard misbehaves, before blaming the code |
| [references/debugging.md](references/debugging.md) | you need Stryker's logs, the injected code, or how CLI options override the config |
| [references/compile-errors.md](references/compile-errors.md) | a CompileError matches no shape in section 5, or a disable comment targets an uncompilable mutant |
| [references/setup.md](references/setup.md) | you edit a config, a shard, the workflow or a test-only host, add a provider, or need a mutator name |
| [references/updating-known-issues.md](references/updating-known-issues.md) | Stryker.NET's version changes, a symptom isn't in known-issues.md, or you're asked to update it |

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
| `identifiers-netstandard` | `tests/OpenUrzednik.Identifiers.NetStandardBranch.TestHost` (no `NET` symbol) | the same, all of it again, built with the netstandard2.0 branches |
| `dates` | `tests/OpenUrzednik.Dates.TestHost` | `src/Dates/*.cs` (the polyfills aren't mutated) |
| `nbp` | `src/OpenUrzednik.Nbp` | validation, URL builders, extensions, models, payload, pipeline, options, `NbpDateOnlyExtensions.cs` (11 globs) |
| `mf-white-list` | `src/OpenUrzednik.MF.WhiteList` | options, validator, `Common/**`, the `WhiteList…Error` types (shared source excluded) |
| `http` | `tests/OpenUrzednik.Http.TestHost` | `src/Http/Infrastructure/**` |
| `http-netstandard` | `tests/OpenUrzednik.Http.NetStandardBranch.TestHost` (no `NET` symbol) | the same, all of it again, built with the netstandard2.0 branches |
| `http-di` | `tests/OpenUrzednik.Http.DependencyInjection.TestHost` | `src/Http/DependencyInjection/**` |
| `core` | `src/OpenUrzednik.Core` | results, `Errors/`, `Exceptions/`, `Validation/` |
| `di`, `logging` | the package's `src/` folder | the whole package |
| `diagnostics` | `src/OpenUrzednik.Diagnostics` | none (ratchet only) |

- Output: `artifacts/mutation/<shard>/reports/mutation-report.{json,html,md}`, plus `artifacts/mutation/summary.md` (git-ignored). `run.py` deletes the old `reports/` first, so a failed run never leaves a stale report behind.
- Exit codes of `run.py`: 0 every shard passed, 1 a gate failed, 2 a shard couldn't run, has no report, tested no mutant, or has a strict glob matching no file.
- Time: about 1–6 minutes per shard on CI including setup, locally of the same order (single-threaded, `concurrency: 1`).
- **Never run two Stryker runs, or Stryker and `dotnet build`/`test`, at the same time in one checkout.** They share `obj/` and `bin/`; corrupted `obj/` has been seen here. For parallel work use a separate `git worktree`. `run.py` also gives each shard its own `TEMP`, because the MTP runner passes the active mutant through a fixed file in the temp folder (`stryker-mutant-<id>.txt`, `Stryker.TestRunner.MicrosoftTestPlatform/MicrosoftTestingPlatformRunner.cs:78`).

### Scoped run (one file, fast loop)

Run Stryker directly from the shard's workdir with the shard's config and an output folder outside the repo (the scratchpad):

```bash
cd src/OpenUrzednik.Nbp
TEMP=<scratch>/tmp TMP=<scratch>/tmp TMPDIR=<scratch>/tmp dotnet stryker --config-file ../../tests/Mutation/stryker-nbp.json \
  -m "**/NbpOptions.cs" --output <scratch>/nbp-options -r json --verbosity warning --skip-version-check
uv run ../../.claude/skills/stryker/references/mutants.py list <scratch>/nbp-options/reports/mutation-report.json
```

One file of the core shard took 41 s this way (2026-11-07); other files' mutants show as `Ignored` (`Removed by mutate filter`), and compile-error collateral is the same as in a full run.

- A CLI option **replaces** the config's value. With `-m` the config's exclusions are gone, so keep the pattern narrow or repeat them; once you pass one `-r`, pass `-r json` too, or the JSON report isn't written. Details: [debugging.md](references/debugging.md).
- A scoped run is for iterating. The gate is `run.py` on the whole shard: run it before the PR when strict code changed (`.claude/rules/testing.md`).
- Agent-friendly flags, adapted from Stryker's own `run-stryker` skill (see the end): `--verbosity warning` (also hides the banner), `--skip-version-check`, `-r json`. Its other suggestion, `-r cleartext`, prints a row for every file of the project, excluded ones included: skip it and read the JSON.

## 2. The gate

`run.py` reads each shard's JSON report and fails it when Stryker exits non-zero, when **no mutant was tested** (Stryker exits 0 when it can't compute a score, PR sn#3672), when a strict glob matches no file, when a mutant in a strict file is **Survived or NoCoverage**, or when the score is below the shard's `break`. Score = (Killed + Timeout) / (Killed + Timeout + Survived + NoCoverage); CompileError, RuntimeError and Ignored don't count (`Mutants/Mutant.cs:26`). Today every shard has `break: 100`.

- **Strict** files: code whose broken behaviour would harm users (ADR-0020 lists them). No undetected mutant may stay. **Ratchet** code: its `break` is the floor of its last score, raised when tests improve, until it turns strict.
- CI runs it as the required check `mutation` on PRs into `main` and `release/*.x` and weekly on `develop`, not on PRs into `develop` that change only `src/`: run the shards locally. Triggers, artifacts and the shared config settings: [setup.md](references/setup.md).

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

1. **Missing test** (the usual case). Write a test that fails with the mutant: follow the testing conventions in `.claude/rules/testing.md` (one partial test class file per method, `Method_Condition_ExpectedResult`, deterministic data, `StubHttpMessageHandler`). Rerun a scoped run and check with `--tests` that the new test is in `killedBy`. NoCoverage means no test reaches the line at all; check first that the code is reachable on `net10.0`.
2. **Equivalent mutant** (no observable change, e.g. a performance-only path or a cache). Prefer **rewriting the code so the mutant doesn't exist**, at the same cost: PR #240 changed `end < 0` after `IndexOf` to `end == -1`, removing the equivalent `<= 0` mutant without hiding the killable `> 0`. Add a short comment citing ADR-0020 so the code doesn't drift back.
3. Only when no such rewrite exists: `// Stryker disable once <Mutator> : <reason>` on the line before the narrowest statement (mutator names: [setup.md](references/setup.md)). Rules (ADR-0020, and the `reviewer` agent checks them):
   - Only for an equivalent mutant or one Stryker can't activate (item 5), never for a missing test.
   - It covers every mutant of the listed kinds in the **whole following statement** (a block statement included). After adding it, diff the reports: only the intended mutants may turn `Ignored`; a killed one turning `Ignored` means the comment hides a test.
4. **Timeout** counts as detected, but the MTP runner produces false timeouts (sn#3956, sn#3915). A loop that no longer ends (a removed `i++`) is a real one; for any other Timeout in strict code, make the change by hand and check that a test fails, or add one.
5. **Static initializers**: the MTP runner reuses one test host across mutants, so a static field initializer's mutant is usually never activated and survives (sn#3897). Check by hand that breaking it fails tests, then disable it with that reason (`src/OpenUrzednik.Nbp/Common/NbpConnection.cs:14`).

Report the before/after counts per shard in the PR, as PRs #237, #240 and #247 do.

## 5. Compile-error collateral (the main pitfall)

Stryker compiles all mutants together. When that fails, it rolls back the mutant **enclosing** each compiler error, even if that one compiles fine, or, finding none, every mutant of the method or file (Safe Mode); all of them become `CompileError` ([compile-errors.md](references/compile-errors.md)). So one uncompilable mutant can silently remove compilable, killable neighbours from testing, and the gate can't see it. Shapes found here and their fixes (PRs #237, #247):

| Shape | Uncompilable mutant | Fix used |
|---|---|---|
| Pattern variable `x is { } y`, `value is int code` | Equality `is not` leaves `y` unassigned where it is used (CS0165); for `{ } y` the ternary also declares `y` twice (CS0128, sn#3597). The mutant around the use (`y + 1` → `y - 1`) is blamed. | `HasValue` + `GetValueOrDefault()` for nullable structs; a local + `is not null` for references |
| Declaration inside a condition (`is { } errors ? … : …`, `TryGet(out var x) ? …`) | Conditional `true`/`false`, Logical, Negate remove or re-scope the declaration | Declare the local (or call `TryGet…`) in its own statement first |
| `list.Count` property (`IReadOnlyList<T>`) | Linq `Count` → `Sum` on a property: a method group (CS0019/CS0173); the comparison around it is blamed | Read `Count` into a local (`var count = errors.Count;`); the local's own `Sum` mutant stays a harmless CompileError |
| `StringBuilder.Append(...)` | Linq `Append` → `Prepend` (no such method) | Leave it: it blames nothing else |
| `string.Create(provider, $"…")` | String `$""` as `IsActive(n) ? $"" : $"…"` loses the `ref` handler conversion (CS1620) | Leave it: genuine, hides nothing |

How to tell genuine from collateral: for each CompileError ask **"would this mutant compile on its own?"** If yes (`errors.Count != 1`, `!=` → `==`, a removed statement), it is collateral; find the uncompilable mutant at or inside the same expression or statement (`mutants.py list --file X --all`, then `--diag --verbosity trace` if unclear) and restructure that code. The remaining CompileErrors must be genuinely uncompilable and must not wrap anything; state that in the PR.

Configuration (`ignore-mutations`, `ignore-methods`) can't free collateral; only a `// Stryker disable` comment acts before compilation. Prefer the code rewrite. A comment is allowed only under ADR-0020's second reason, a mutant Stryker can't activate: its reason must say the mutant can't compile, and the report diff must show that it hides no mutant a test kills.

## 6. MTP and xUnit v3 specifics

- `test-runner: mtp` is required (VSTest can't run xUnit v3) and is a preview: if a Stryker bug fails a shard, rerun it, and if it persists, fix or pin the tool before releasing. `concurrency: 1` stays until sn#3897 ships (sn#3832: parallel runs under-report kills).
- Only `net10.0` is mutated; Stryker can't run the net472 tests (MTP: sn#3421; VSTest with xUnit v3 activates no mutant, sn#3920). The netstandard2.0 branches of shared source are mutated anyway: the `-netstandard` hosts compile them on net10.0 without the `NET` symbol ([setup.md](references/setup.md), ADR-0020). A test there runs on .NET's runtime, so a branch only .NET Framework reaches (`HttpContentJson`'s `null` content) needs a direct test. `Polyfills/` and net8.0-only branches aren't mutated; the net472 test jobs run the real netstandard2.0 build.
- Stryker 5 needs the .NET 10 runtime, and restore needs the newest SDK. Restore the solution before running: without restored packages Stryker's design-time build drops test projects silently (fewer tests, false NoCoverage). CI does this.
- A run that tested nothing exits 0 (in a shallow clone, Nerdbank.GitVersioning makes every mutant a RuntimeError); check the summary, not only the exit code, when running Stryker by hand. Keep the test suite green: under MTP one failing test aborts the run.

Every other known bug, with its state in 5.0.0 and the workaround used here: [references/known-issues.md](references/known-issues.md). Check it before blaming the code, and add to it when you find a new one.

## 7. Keep the known issues current

Update [references/known-issues.md](references/known-issues.md) when Stryker.NET changes version (in that PR, before it is merged), when a shard misbehaves with a symptom it doesn't list, or on request. Follow [references/updating-known-issues.md](references/updating-known-issues.md); never post upstream yourself.

---

License: MIT ([LICENSE](LICENSE)); copy and adapt it freely. Option advice in section 1 is adapted from Stryker.NET's `run-stryker` agent skill (`.github/skills/run-stryker/SKILL.md`, stryker-mutator/stryker-net at `dotnet-stryker@5.0.0`, commit 6e77a3451bac4793e9c839c3ff4055c1b30ca3af), Apache License 2.0; see the notice in [LICENSE](LICENSE).
