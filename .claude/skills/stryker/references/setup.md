# Shards, configs and CI

Part of `/stryker` ([SKILL.md](../SKILL.md)). [ADR-0018](../../../../docs/adr/0018-mutation-testing-before-release.md) is binding; this file covers the CI triggers, shards, configs and mutator names that apply it. Read it when you edit a Stryker config, a shard, the mutation workflow or a test-only host, or add a provider.

## CI

- `.github/workflows/mutation.yml`: required check `mutation` on PRs into `main` and `release/*.x`; Monday 02:00 UTC on `develop`; `workflow_dispatch`.
- `mutation-tooling.yml` calls it on PRs into `develop` that touch `tests/Mutation/**`, the test hosts, the workflows or `.config/dotnet-tools.json`.
- A PR into `develop` that changes only `src/` doesn't run it: run the shards locally, and if CI evidence is wanted, dispatch it on the branch (`gh workflow run mutation.yml --ref <branch>`; not read-only, so ask first).
- Reports are the `mutation-<runner>` artifacts (30 days).
- Time: each shard job takes about 1–6 minutes including setup (`nbp` and `http` are the longest; runs 42355286460, 42403860292).

## Shards and test-only hosts

One shard per project under test, one CI runner per shard (`adapters` runs `di logging diagnostics`). Stryker never mutates a test project's own files, so shared source that only tests compile lives in test-only host libraries (`tests/OpenUrzednik.Identifiers.TestHost`, `…Http.TestHost`, `…Http.DependencyInjection.TestHost`) and is mutated once, in the host's shard; provider and DI shards exclude it (`!**/Http/**`). A new provider adds its shards (`/new-provider`).

Test-only hosts set `IsTestProject=false` and get no xUnit/MTP packages; a project referencing Microsoft.Testing.Platform is taken for a test project (sn#3887).

## Configs

Every config (`tests/Mutation/stryker-<shard>.json`) shares: `test-runner: mtp`, `target-framework: net10.0`, `concurrency: 1`, `coverage-analysis: perTest`, `mutation-level: Standard`, `mutate` without `Polyfills/` and `Dto/`, `ignore-methods` `*Log`, `LogUnreadableBody`, `*Exception.ctor` (log calls and exception constructor arguments aren't contract, ADR-0018), reporters html, json, markdown, progress. Keep them aligned; only `project`, `test-projects` and extra exclusions differ.

## Mutator names

For `// Stryker disable once <Mutator> : <reason>`, the names are the enum names (`Stryker.Abstractions/Mutator.cs`): `Statement`, `Arithmetic`, `Block`, `Equality`, `Boolean`, `Logical`, `Assignment`, `Unary`, `Update`, `Checked`, `Linq`, `String`, `Bitwise`, `Initializer`, `Regex`, `NullCoalescing`, `Math`, `StringMethod`, `Conditional`, `CollectionExpression`; several are comma-separated. Examples: `src/Http/Infrastructure/RestRequestExecutor.cs:486`, `src/OpenUrzednik.Nbp/Common/NbpConnection.cs:14`.
