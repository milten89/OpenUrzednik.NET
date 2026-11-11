# CLAUDE.md

Instructions for AI coding agents working in this repository. Also read [`docs/adr/`](docs/adr/README.md): accepted and released ADRs are binding, and this file summarizes them.

The rest of each area's rules are in [`.claude/rules/`](.claude/rules/). Claude Code loads a rule file when you work with a file its `paths:` match; other agents find the file for each path in [`AGENTS.md`](AGENTS.md). The lines kept here are the ones you need before you open any file.

## Project

OpenUrzednik.NET is a set of unofficial .NET client libraries for Polish public APIs, published to NuGet as separate packages.

| Project | State |
|---|---|
| `src/OpenUrzednik.Core` | Result pattern (`OpenUrzednikResult`, `OpenUrzednikResult<T>`), errors and exceptions, telemetry abstractions, `ValueValidator<T>` |
| `src/Http` | Shared REST/JSON layer, not a package: internal source compiled into each REST provider through `Http.props` (a DI package links `Infrastructure/ResilienceRejection.cs` to mark resilience rejections; a DI package of a provider with display paths imports `DependencyInjection/DependencyInjection.props`, its HttpClient logging, ADR-0017) (`RestRequestExecutor`, `RestProviderProfile`, `RestOptions`, `OpenUrzednikTelemetry`; namespace `OpenUrzednik.Http.Infrastructure`); tested in `OpenUrzednik.Http.Tests` |
| `src/Privacy` | Masking of personal identifiers and secrets (ADR-0003), not a package: internal source compiled into each provider that handles them through `Privacy.props` (`SensitiveValue.Mask`; namespace `OpenUrzednik.Privacy`); tested in `OpenUrzednik.Core.Tests` through `OpenUrzednik.Identifiers.TestHost` |
| `src/Identifiers` | Validators of Polish identifiers, not a package: internal source compiled into each provider that takes them through `Identifiers.props`, which brings `src/Privacy` (`NipValidator`, `RegonValidator`, `KrsNumberValidator`, `BankAccountNumberValidator`, `IdentifierNormalizer`; namespace `OpenUrzednik.Identifiers`); tested in `OpenUrzednik.Core.Tests` |
| `src/Dates` | Date helpers, not a package: internal source compiled into each provider that takes or returns dates through `Dates.props`, which also adds the global using and, on netstandard2.0, the `DateOnly` alias (`DateOnlyExtensions.ToPublicDate`/`ToIso8601String`, `WarsawCalendar.Today`, the `DateOnly` polyfills; namespace `OpenUrzednik.Dates`); tested in `OpenUrzednik.Core.Tests` |
| `src/OpenUrzednik.Extensions.Logging` | `IOpenUrzednikLogger` over `ILogger` (`OpenUrzednikLogger`, `CreateOpenUrzednikLogger<TClient>()`) |
| `src/OpenUrzednik.Diagnostics` | `IOpenUrzednikTraceSource` over `ActivitySource` (`ActivityTraceSource`, `GetShared("OpenUrzednik.<Provider>")`) |
| `src/OpenUrzednik.Nbp` | NBP API: currency rates (`Currency/`), rate tables (`Table/`), gold prices (`Gold/`). **Reference provider**, still being hardened |
| `src/OpenUrzednik.Nbp.DependencyInjection` | `services.AddOpenUrzednikNbp()`: the NBP clients as typed clients of one named `HttpClient`, validated options, adapters wired; returns the `IHttpClientBuilder`. `AddKeyedOpenUrzednikNbp(key)`: the same as keyed services, one setup per key |
| `src/OpenUrzednik.MF.WhiteList` | MF VAT White List API (Wykaz podatników VAT, [ADR-0023](docs/adr/0023-mf-white-list-provider.md)): skeleton, not packable yet (`IsPackable=false`, no client methods). `WhiteListOptions` (`ApiUrl` = host root; clients add `api/` or `api2/`), `Common/WhiteListConnection` (executors and the `mf_white_list` profile), `Common/WhiteListErrorMapping` (`{message, code}` bodies to the provider errors, echoed identifiers masked), `Common/WhiteListRequestPipeline` (display paths always), the error types (`WhiteList…Error`, `IWhiteListErrorCode`, keys in `WhiteListErrorMetadata`); imports `src/Http`, `src/Identifiers`, `src/Dates` |
| `src/OpenUrzednik.Transport.Http` | The public HTTP transport types (ADR-0021): `Http…Error` subclasses of Core's condition errors, not sealed so providers derive their own details from them (`HttpNotFoundError : NotFoundError, IOpenUrzednikHttpStatusCode`, …) and `IOpenUrzednikHttpStatusCode`; every REST provider depends on it through `src/Http/Http.props`; tested in `OpenUrzednik.Transport.Http.Tests` |
| `tests/OpenUrzednik.*.Tests` | Unit tests (xUnit v3, Shouldly, NSubstitute, Bogus, `FakeTimeProvider`) |
| `tests/OpenUrzednik.IntegrationTests` | WireMock tests (run in CI) and tests against the real API (explicit: they run with `--explicit on` or when `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` is set) |
| `tests/OpenUrzednik.AotSmoke` | Native AOT smoke app, not an xUnit project: published and run by the CI job `aot (net10.0)` (`dotnet publish tests/OpenUrzednik.AotSmoke -f net10.0`, needs `clang` on Linux or the C++ build tools on Windows) |
| `tests/OpenUrzednik.TestCommon` | Shared test helpers: `StubHttpMessageHandler`, `ManualFact`/`ManualTheory`, Faker extensions |
| `tests/OpenUrzednik.Identifiers.TestHost`, `tests/OpenUrzednik.Dates.TestHost`, `tests/OpenUrzednik.Http.TestHost`, `tests/OpenUrzednik.Http.DependencyInjection.TestHost` | Test-only libraries that compile shared source on its own (`src/Identifiers` with `src/Privacy`; `src/Dates`; `src/Http/Infrastructure`; `src/Http/DependencyInjection`), so Stryker.NET mutates it in the host's shard and never in a provider's; Core.Tests and Http.Tests reference them instead of compiling copies or reaching a provider's copy |
| `tests/OpenUrzednik.Identifiers.NetStandardBranch.TestHost`, `tests/OpenUrzednik.Http.NetStandardBranch.TestHost` and their `….Tests` | The same shared source compiled without the `NET` symbol (`DisableImplicitFrameworkDefines`), so its netstandard2.0 branches compile on net10.0 and Stryker.NET mutates them (shards `identifiers-netstandard`, `http-netstandard`, [ADR-0020](docs/adr/0020-mutation-testing-of-every-target-branch.md)); the test projects link Core.Tests' `Identifiers/` and `Privacy/` and Http.Tests' `Infrastructure/` |
| `tests/Mutation` | Stryker.NET mutation shards (`shards.json`, `stryker-<shard>.json`) and `run.py`, the gate ([ADR-0020](docs/adr/0020-mutation-testing-of-every-target-branch.md)) |
| `samples/` | Runnable examples against the real NBP API (console, minimal Web API with DI); built in CI with the solution, not packed |

Work is tracked in GitHub issues and the project [OpenUrzednik.NET](https://github.com/users/milten89/projects/2); see "Issues and the project" below. Pick work from there unless told otherwise.

## Commands

```bash
dotnet build OpenUrzednik.slnx
dotnet test OpenUrzednik.slnx                    # all TFMs; locally only installed runtimes work
dotnet test OpenUrzednik.slnx -f net10.0         # fastest local loop
dotnet test tests/OpenUrzednik.Nbp.Tests --filter-class "*NbpGoldPriceClientTest"
dotnet format OpenUrzednik.slnx --verify-no-changes   # CI fails if this reports changes
dotnet pack OpenUrzednik.Packages.slnf -c Release    # runs package validation, as CI does
dotnet test tests/OpenUrzednik.IntegrationTests --explicit on   # also hits the real NBP API
dotnet tool restore                                             # once: Stryker.NET (local tool)
uv run tests/Mutation/run.py nbp                                # mutation tests of one shard (--all, --list); reports in artifacts/mutation
```

Before you say a task is done, run `dotnet build OpenUrzednik.slnx -p:TreatWarningsAsErrors=true` (every warning fails, as in CI; the NuGet audit warnings stay warnings), `dotnet test OpenUrzednik.slnx -f net10.0` and `dotnet format OpenUrzednik.slnx --verify-no-changes`, and report the results.
If the change touches a public API, a target or packaging, also run the pack above: package validation fails it when the builds of a package differ.
Package validation also compares each package with the latest release (`PackageValidationBaselineVersion` in `src/Directory.Build.props`, raised by the back-merge after each release).
Never add a suppression (`CompatibilitySuppressions.xml`, `NoWarn` of a `CPxxxx`); ADR-0011 allows only CP0006 for a new client-interface method, and baseline suppressions (`IsBaselineSuppression`) for breaking changes while `version.json` carries a new major not released yet.

Run `dotnet format` only with `--verify-no-changes`, and only on a tree that builds (the format hook's `dotnet format whitespace --folder` pass on each edited `.cs` file is the exception: it needs no build and changes only whitespace): its fix mode inserted casts into code that didn't compile, and wrote merge-conflict markers into multi-target projects whose targets disagreed about unnecessary usings.
To remove unused usings, run `dotnet build --no-incremental`, collect the IDE0005 warnings per target framework, and delete only the lines flagged on every target.

Look APIs up instead of relying on memory: .NET and other Microsoft APIs on Microsoft Learn (the `microsoft-learn` server in `.mcp.json`, or learn.microsoft.com), including the "Applies to" list for netstandard2.0 and .NET Framework; library APIs with Context7 (the `context7` server) for the version in `Directory.Packages.props`; a provider's API with `/verify-api`.

## Architecture rules (from ADRs)

**Errors ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md))**
- Public async APIs return `Task<OpenUrzednikResult<T>>`. Failures that can happen during normal execution are returned, not thrown: validation, every non-success HTTP status, bad payloads, network errors, timeouts.
- **Never write `catch (Exception)` or a bare `catch`.** Catch specific types and convert them to errors. CA1031 flags it in `src/` (`src/.editorconfig`).
- More rules: [`.claude/rules/errors.md`](.claude/rules/errors.md).

**Dependencies ([ADR-0004](docs/adr/0004-dependency-policy.md))**
- Core and provider packages take **no package dependencies** on .NET targets.
- **If a change raises an abstraction above its minimum** (typically a Dependabot PR), stop and ask the maintainer explicitly what to do, naming the package, both versions and the rule. Don't merge, approve, rebase onto or build on that change until they answer explicitly.
- Do not add a `PackageReference` to a `src/` project without an ADR or explicit approval. The build fails on a package the project's list doesn't name (`CheckPackageReferenceAllowList` in `src/Directory.Build.targets`; the integration packages and Diagnostics set `OpenUrzednikAllowedPackageReferences` in their csproj). Never extend a list to make a build pass.
- More rules: [`.claude/rules/dependencies.md`](.claude/rules/dependencies.md).

**Shared internal code ([ADR-0021](docs/adr/0021-transport-subclasses-of-condition-errors.md), [ADR-0019](docs/adr/0019-shared-http-layer-tested-through-test-host.md))**
- Internal code that several packages need is shared source: a `src/<Area>` folder with a `.props` file that each package imports, tested and mutated through a test-only host (`src/Http`, `src/Identifiers`, `src/Privacy`, `src/Dates`).
- Never move such code into a package that others reach through `InternalsVisibleTo`, and never make it public for that reason: an app can pair packages of different versions, so internals would become a cross-package contract, and on netstandard2.0 the polyfills would clash (CS0436). Core's telemetry internals (ADR-0003) are the one existing exception.
- Types that apps use belong in a real package, such as `OpenUrzednik.Transport.Http`.

**Telemetry ([ADR-0003](docs/adr/0003-telemetry-abstractions.md))**
- Use `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` from Core.
  - Don't use `ILogger` or `ActivitySource` in Core or provider packages.
  - The one exception is the fallback trace source: on the .NET targets a client without a trace source uses the shared `OpenUrzednik.<Provider>` source, through Core's internal `Telemetry/Activities` (compiled into Core on .NET only, linked into `OpenUrzednik.Diagnostics` for netstandard2.0; Core grants `InternalsVisibleTo` on .NET only, and a new REST provider must be added to it). netstandard2.0 falls back to `NullOpenUrzednikTraceSource`.
  - Those Core internals are called across packages, and an app may pair a newer Core with older packages: within a major, add to them, never change or remove.
- These interfaces are frozen from 1.0: a new capability is a separate interface that callers check with `is`.
- More rules: [`.claude/rules/telemetry.md`](.claude/rules/telemetry.md).

**Target frameworks ([ADR-0005](docs/adr/0005-target-frameworks.md))**
- Libraries target `netstandard2.0` plus the .NET LTS releases in support, today `netstandard2.0;net8.0;net10.0` (the .NET targets are in the root `Directory.Build.props`); .NET Framework 4.7.2 or later uses netstandard2.0; tests add `net472`, which runs the netstandard2.0 build on .NET Framework (CI job `test (net472)`; xUnit v3, package version 4, supports nothing older). A .NET target that isn't the lowest one may be removed in a minor version once Microsoft ends its support; the lowest .NET target and netstandard2.0 are removed only in a major.
- More rules: [`.claude/rules/target-frameworks.md`](.claude/rules/target-frameworks.md).

**HTTP ([ADR-0019](docs/adr/0019-shared-http-layer-tested-through-test-host.md))**
- All REST/JSON provider HTTP goes through `RestRequestExecutor` (`GetAsync`) in the shared HTTP layer (`src/Http`); those providers never call `HttpClient.SendAsync`; a SOAP or other transport follows its provider ADR.
- Deserialize with source-generated `JsonTypeInfo<T>` (`NbpJsonContext`); no reflection-based serialization.
- More rules: [`.claude/rules/http.md`](.claude/rules/http.md).

**Compatibility ([ADR-0011](docs/adr/0011-compatibility-policy.md))**
- Public classes not designed for inheritance are `sealed` (unsealing is a minor change, sealing a major one). The abstract extension points (`OpenUrzednikError`, `OpenUrzednikException`, `ValueValidator<T>`) never get a new abstract member, and no interface except a client interface gets a new member (telemetry interfaces are frozen).
- Since 1.0.0:
  - Covered within a major: the public API on every target, documented behaviour, the error contract (ADR-0002) and the telemetry contract (ADR-0003). Not covered: log and exception message text, log properties, undocumented behaviour, internal types (except Core internals called through `InternalsVisibleTo`, which are only added to), previews.
  - A client interface may gain a method in a minor version; package validation reports it (CP0006) once the package has been released, and the PR suppresses exactly that diagnostic, citing ADR-0011.
  - Before a major removes a member, mark it `[Obsolete]` (with the replacement) for at least one minor. A patch (`hotfix/*`) adds no public API.
  - A PR with a breaking change carries `breaking-change` and raises `version.json` on `develop` to the next major ([ADR-0008](docs/adr/0008-branching-versioning-and-release.md)).
- Support ([ADR-0013](docs/adr/0013-version-support-policy.md)): fixes go to the latest minor of the latest major; after a new major, the previous one gets security fixes for 6 months from `release/N.x` (ADR-0008).

**Client API ([ADR-0007](docs/adr/0007-client-api-and-extensibility.md))**
- The default case is one line (`new NbpGoldPriceClient(httpClient)`).
- `CancellationToken cancellationToken = default` is the last parameter of every async method, in both interfaces and implementations.
- More rules: [`.claude/rules/client-api.md`](.claude/rules/client-api.md).

## Performance

- **Strongly prefer the faster code over the more convenient one** in library code (`src/`). Readability matters, but not at the cost of performance on a path that runs per request, per log entry or per item: use spans instead of `Substring`, avoid per-call allocations (arrays built from constants, closures, boxing, LINQ, string concatenation), and prefer generic APIs to reflection.
- More rules: [`.claude/rules/performance.md`](.claude/rules/performance.md).

## Code conventions

- C# `latest`, nullable enabled, file-scoped namespaces, `_camelCase` private fields. `.editorconfig` is authoritative.
- No parallel API variants: one public method per operation, returning a result; the exception style comes only through `EnsureSuccess()` / `EnsureSuccessAsync()`.
- More rules: [`.claude/rules/code-conventions.md`](.claude/rules/code-conventions.md).

## Testing conventions

- Tests against the real API use `[ManualFact]`/`[ManualTheory]` (explicit tests) and must never run in default CI.
- Tests run on Microsoft.Testing.Platform (`global.json`), so filters are xUnit's (`--filter-class`, `--filter-method`, `--filter-trait`), not VSTest's `--filter`. Filter one project: on the whole solution, projects with no matching test make the run exit with code 8 (add `--ignore-exit-code 8`).
- More rules: [`.claude/rules/testing.md`](.claude/rules/testing.md).

## Workflow

- Branch from `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`). PRs target `develop` and are merged with a merge commit (squash is disabled). Releases go from a `release/X.Y` branch, made from `develop`, into `main` ([ADR-0008](docs/adr/0008-branching-versioning-and-release.md)).
- Prepare a release only with `/prepare-release` (it never publishes: the maintainer merges the PR into `main` and approves the `nuget-release` deployment); after the publish, `/prepare-release after` checks it and opens the back-merge.
- A release or hotfix PR into `main`, or a backport PR into a support branch `release/N.x`, must add `docs/release-notes/v<version>.md` (`/release-notes`): the `build (net10.0)` job checks it on the PR, and `nuget-release.yml` checks it again before publishing.
- Bug fixes in released code ([ADR-0013](docs/adr/0013-version-support-policy.md)): a fix that isn't critical goes into `develop` and ships in the next release; a critical one ships as a hotfix of the current version.
  - The maintainer decides at triage and writes "hotfix" in the issue. Without that, target `develop`.
  - Critical means, for example: wrong data returned, a throw where a result is promised (ADR-0002), a target framework that breaks, identifiers leaking into logs, a regression from the last release.
  - A hotfix follows [`docs/RELEASE-PROCESS.md`](docs/RELEASE-PROCESS.md): `hotfix/…` from the `vX.Y.Z` tag, the next patch in `version.json`, a PR into `main`, then the back-merge into `develop` through a `merge/vX.Y.Z` branch. It adds no public API, so a fix that needs one waits for the next minor.
  - A fix judged critical after it merged into `develop` is cherry-picked into a `hotfix/…` branch made from the tag.
  - A vulnerability never goes into a public issue or PR first: report it privately ([`SECURITY.md`](SECURITY.md)), and the maintainer prepares the fix in a private advisory.
- After a new major, the previous one has a support branch `release/N.x` (a public ref; [ADR-0008](docs/adr/0008-branching-versioning-and-release.md), [ADR-0013](docs/adr/0013-version-support-policy.md)) for security fixes only, for 6 months. A security fix lands on the latest line first, then is backported: `hotfix/N.x-…` from `origin/release/N.x`, cherry-pick the fix, the next patch of that line, release notes, a PR into `release/N.x`. Never merge `release/N.x` into `main` or `develop`. Nothing else goes into it, except a CI fix needed to keep it releasable, in the same backport PR.
- Commit or push only when asked. One issue per PR (see "Issues and the project").
- Stage explicit paths (`git add <paths>`), never `git add -A`, `git add .` or `git commit -a`: a checkout can hold local files that aren't part of the change.
- Change only what the issue needs: no drive-by refactors, renames or reformatting. Related consistency updates (docs, skills, the project table, `README.pl.md`, cross-references) belong in the same PR; list the optional ones in the PR for the maintainer to decide.
- ADR lifecycle ([ADR-0022](docs/adr/0022-record-architecture-decisions-frozen-at-release.md)): `proposed` → `accepted` (approved, binding on `develop`) → `released` (`released-in: vX.Y.Z`, set by `/prepare-release`).
  - An `accepted` ADR that no release has shipped is corrected in place through `/adr`, with a dated line under More Information.
  - Never edit a `released`, rejected or superseded ADR except its status line, `released-in` and a link to the ADR that supersedes it.
  - Follow-up work goes into issues; a clarification that changes no decision goes here, into `.claude/rules/` or into the linked docs.
  - If a change contradicts an ADR, stop and use `/adr` instead of working around it: correct an `accepted` one in place, or write a new ADR that restates the whole decision and supersedes a `released` one.
  - A decision that shapes the architecture or is hard to reverse (public API shape, the error or telemetry contract, dependencies, target frameworks, the release and support flow) gets an ADR in the same PR, also when it was settled in chat.
- Before opening a PR, run the `reviewer` agent on the diff.
- After opening it, wait for the `CodeQL` check in `gh pr checks <n>` (the `Analyze (…)` jobs pass even when they find something), read the open alerts with `gh api "repos/milten89/OpenUrzednik.NET/code-scanning/alerts?ref=refs/pull/<n>/merge&state=open"`, and fix them in the same PR.
- Packaging, package icons and required CI job names: [`.claude/rules/packaging-and-ci.md`](.claude/rules/packaging-and-ci.md).
- Documentation language and prose style: [`.claude/rules/docs.md`](.claude/rules/docs.md).

## Issues and the project

Work is tracked in this repository's issues and in the public project [OpenUrzednik.NET](https://github.com/users/milten89/projects/2); its fields, views and workflows are described in [`docs/GITHUB-SETUP.md`](docs/GITHUB-SETUP.md#project). The issue body is the specification of the work.

- Take open issues with Status `Ready` and without the `maintainer-only` label, by Priority, then Seq. Only the maintainer decides that an issue moves to `Ready` (an issue opened for work the maintainer asked for counts as decided; see `/triage`).
- Before you start, check that no open issue blocks it: `gh issue view <n> --json blockedBy --jq '[.blockedBy.nodes[] | select(.state == "OPEN") | .number]'` must print `[]`.
- When you start, set the issue's Status to `In progress`. That is the only field you change: never set `Done`, Priority, Phase, Seq, Size, Due, or the issue's milestone and labels unless asked.
- One issue per PR. The PR body starts with `Closes #<n>`. When you open the PR, give it at least one release label from `.github/release.yml` (`gh pr create --label <label>`), normally the one forecast on the issue or proposed by the `reviewer` agent; the `release-label` check fails a PR without one.
- Never start, change or close a `maintainer-only` issue.
- You may open follow-up issues for problems outside the current issue. The project adds them with Status `Triage`, and they are sorted with `/triage`, after the maintainer approves. An issue opened for work the maintainer asked for is triaged at once (`/triage`, last section).
- Release and hotfix PRs target `main` (backports a support branch `release/N.x`), where `Closes` doesn't close the issue. The maintainer moves those issues to `Done`.

Project fields need the `project` scope (`gh auth refresh -s project`):

```bash
# Ready issues for agents, by Priority, then Seq (.id is the project item id)
gh project item-list 2 --owner milten89 --format json --limit 200 --jq '[.items[] | select(.status == "Ready" and .content.type == "Issue" and ((.labels // []) | index("maintainer-only") | not))] | sort_by(({"Urgent":0,"High":1,"Medium":2,"Low":3}[.priority // ""] // 9), .seq) | .[] | "#\(.content.number) \(.priority) \(.id) \(.title)"'
# Set Status to In progress
gh project item-edit --project-id PVT_kwHOAJh3Rs4BnebI --id <item id> --field-id PVTSSF_lAHOAJh3Rs4BnebIzhmC35A --single-select-option-id e0ec58a2
```

## Contributors without write access

Everything in this file and in `.claude/rules/` applies to you; only these steps differ.

- Work in a fork: clone it, add this repository as the `upstream` remote, run `gh repo set-default milten89/OpenUrzednik.NET`, and branch from `upstream/develop`. Where a skill or the `reviewer` agent says `origin/…`, use `upstream/…`.
- A bug fix always targets `develop`, also when the issue says hotfix: the maintainer takes care of the hotfix part.
- Don't set project fields, labels or milestones, and skip the project commands above: comment on the issue that you're working on it. The maintainer sets the release label on your PR, and the `release-label` check waits for it.
- Open the PR with `gh pr create -R milten89/OpenUrzednik.NET --base develop --head <your-user>:<branch>`. The `reviewer` agent diffs against `upstream/<base>` when that remote exists. If `gh api` can't read the code-scanning alerts, open the `CodeQL` check's details on the PR.
- Where a rule says to stop and ask the maintainer, ask in an issue comment or a draft PR.

## Skills and agents

- `/new-provider`: checklist for a new provider package and its DI package: every place in the repository a provider touches (projects, solution filter, telemetry, AOT smoke app, icon, release filters and scripts, READMEs, issue forms).
- `/new-endpoint`: checklist for adding or changing a client method (DTO, mapper, validation, client, tests, docs).
- `/stryker`: run, read and fix the mutation tests (shards, scoped runs, reports, survivors, compile-error collateral) and the known Stryker.NET bugs with their workarounds; update its known issues when Stryker.NET's version changes (MIT, with an Apache-2.0 part).
- `/verify-api`: compare DTOs and test payloads with live API responses, and capture fixtures; each provider adds its notes (endpoints, error cases, quota, test environment) to the skill.
- `/adr`: create a new MADR record, correct an accepted one in place, or supersede a released one, and update the index.
- `/release-notes`: draft `docs/release-notes/v<version>.md` for a release or hotfix; `nuget-release.yml` publishes nothing without it.
- `/prepare-release`: prepare a release (checks, `release/X.Y`, version, notes, package checks, the PR into `main`); `after`: verify the published release and open the back-merge.
- `/triage`: sort the project's Triage column (verify each issue against `develop`, propose the fields in one table, apply them after the maintainer approves); also how an agent triages an issue it opens at the maintainer's request.
- `/miodkuj`: remove AI-sounding, bureaucratic and translated phrasing from Polish prose (vendored from [bartekpucek/miodkuj@32004e3](https://github.com/bartekpucek/miodkuj/tree/32004e3), MIT).
- `/stop-slop`: remove predictable AI writing patterns from English prose (vendored from [hardikpandya/stop-slop@8da1f03](https://github.com/hardikpandya/stop-slop/tree/8da1f03), MIT).
- `reviewer` agent: reviews a diff against the ADRs and the conventions in this file and `.claude/rules/`.
