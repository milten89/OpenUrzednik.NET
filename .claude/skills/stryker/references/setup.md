# Shards, configs and CI

Part of `/stryker` ([SKILL.md](../SKILL.md)). [ADR-0020](../../../../docs/adr/0020-mutation-testing-of-every-target-branch.md) is binding; this file covers the CI triggers, shards, configs and mutator names that apply it. Read it when you edit a Stryker config, a shard, the mutation workflow or a test-only host, or add a provider.

## CI

- `.github/workflows/mutation.yml`: required check `mutation` on PRs into `main` and `release/*.x`; Monday 02:00 UTC on `develop`; `workflow_dispatch`.
- `mutation-tooling.yml` calls it on PRs into `develop` that touch `tests/Mutation/**`, the test hosts, the workflows or `.config/dotnet-tools.json`.
- A PR into `develop` that changes only `src/` doesn't run it: run the shards locally, and if CI evidence is wanted, dispatch it on the branch (`gh workflow run mutation.yml --ref <branch>`; not read-only, so ask first).
- Reports are the `mutation-<runner>` artifacts (30 days).
- Time: each shard job takes about 1–6 minutes including setup (`nbp` and `http` are the longest; runs 42355286460, 42403860292).

## Shards and test-only hosts

One shard per project under test, one CI runner per shard (`adapters` runs `di logging diagnostics`; `identifiers` also runs `identifiers-netstandard` and `dates`, `http` also runs `http-netstandard`). Stryker never mutates a test project's own files, so shared source that only tests compile lives in test-only host libraries (`tests/OpenUrzednik.Identifiers.TestHost`, `…Dates.TestHost`, `…Http.TestHost`, `…Http.DependencyInjection.TestHost`) and is mutated in the host's shard; provider and DI shards exclude it (`!**/Http/**`, `!**/Dates/**`). A new provider adds its shards (`/new-provider`).

The netstandard2.0 branches (`#if NET` … `#else`) get their own hosts (ADR-0020): `tests/OpenUrzednik.Identifiers.NetStandardBranch.TestHost` and `…Http.NetStandardBranch.TestHost` compile the same source with `DisableImplicitFrameworkDefines` (no framework symbols: no `NET`, `NETCOREAPP`, `NET10_0_OR_GREATER`, …), and `….NetStandardBranch.Tests` link the existing test files with the same property, so a test's own `#if NET` expectation follows the branch. The shard mutates the whole source again, not only the `#else` branches, which roughly doubles its runner's time. A linked test must not use `#if NET` for a runtime difference (e.g. .NET Framework's missing inner `TimeoutException`): its `#else` expectation would run on .NET's runtime here. Their configs differ from the default shard's only in `project` and `test-projects`. Things to know:

- The branches run on .NET's runtime. A branch only .NET Framework reaches needs a test that calls it directly (`HttpContentJsonTest`: `null` content).
- Obsolete .NET APIs that a netstandard2.0 branch uses need `NoWarn` in the host (`CS0618` for `HttpRequestMessage.Properties`).
- Polyfills aren't reached: the real .NET member wins. A provider's whole netstandard2.0 build can be hosted the same way (with the `DateOnly` alias); the NBP spike in #251 found one mutant on a netstandard2.0-only line, so it has no shard.

Test-only hosts set `IsTestProject=false` and get no xUnit/MTP packages; a project referencing Microsoft.Testing.Platform is taken for a test project (sn#3887).

## Configs

Every config (`tests/Mutation/stryker-<shard>.json`) shares: `test-runner: mtp`, `target-framework: net10.0`, `concurrency: 1`, `coverage-analysis: perTest`, `mutation-level: Standard`, `mutate` without `Polyfills/` and `Dto/`, `ignore-methods` `*Log`, `LogUnreadableBody`, `*Exception.ctor` (log calls and exception constructor arguments aren't contract, ADR-0020), reporters html, json, markdown, progress. Keep them aligned; only `project`, `test-projects` and extra exclusions differ.

## Mutator names

For `// Stryker disable once <Mutator> : <reason>`, the names are the enum names (`Stryker.Abstractions/Mutator.cs`): `Statement`, `Arithmetic`, `Block`, `Equality`, `Boolean`, `Logical`, `Assignment`, `Unary`, `Update`, `Checked`, `Linq`, `String`, `Bitwise`, `Initializer`, `Regex`, `NullCoalescing`, `Math`, `StringMethod`, `Conditional`, `CollectionExpression`; several are comma-separated. Examples: `src/Http/Infrastructure/RestRequestExecutor.cs:486`, `src/OpenUrzednik.Nbp/Common/NbpConnection.cs:14`.
