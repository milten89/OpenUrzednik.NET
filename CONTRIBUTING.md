# Contributing to OpenUrzednik.NET

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), 10.0.100 or a later 10.0 feature band (`global.json` pins the SDK CI uses; a .NET 11 SDK alone won't build the repository), and the .NET 8 runtime to run the `net8.0` tests.
- For the `net472` tests: Windows with .NET Framework 4.7.2 or later (CI runs them on 4.8). Building needs no .NET Framework targeting pack; the SDK brings the reference assemblies.
- For the mutation tests: [uv](https://docs.astral.sh/uv/) or Python 3.12 or later, and `dotnet tool restore` once (Stryker.NET is a local tool).
- For the Native AOT smoke app (`tests/OpenUrzednik.AotSmoke`; CI runs it, locally it's optional): clang on Linux, or the C++ build tools on Windows.
- The [GitHub CLI](https://cli.github.com/) (`gh`), if you or your agent work with issues and PRs from the command line.
- No Docker: the WireMock tests run WireMock.Net inside the test process.
- With Claude Code: its format hook (`.claude/settings.json`) runs in bash, which is Git Bash on Windows, and uses jq when it's installed.
- For the script tests (`tests/Scripts`, only when you change a script or a workflow step): Linux, or WSL 2 on Windows; see [Testing scripts and workflow steps](#testing-scripts-and-workflow-steps).

## Before you start

- Check the [issues](https://github.com/milten89/OpenUrzednik.NET/issues) and the [project](https://github.com/users/milten89/projects/2): someone may already be working on it. Comment on the issue before you start.
- For larger changes (a new package, a change to the public API), open an issue with a proposal before you write code. It saves time on both sides.
- New providers follow [ADR-0016](docs/adr/0016-next-providers-and-package-naming.md): the next ones are the VAT White List, KRS and GUS BIR, tracked in issues; each starts with a provider ADR, and package names follow the ADR's rules. To propose another data source, use the provider proposal issue form.

## Repository conventions

Binding architecture decisions are in [`docs/adr/`](docs/adr/README.md), and current work and priorities are in the [issues](https://github.com/milten89/OpenUrzednik.NET/issues) and the [project](https://github.com/users/milten89/projects/2). A change that contradicts an ADR, or makes a new architectural decision, needs a new or corrected ADR in the same PR: an accepted ADR is corrected in place until a release ships it, and a released one is never edited ([ADR-0022](docs/adr/0022-record-architecture-decisions-frozen-at-release.md)). All rules in [`CLAUDE.md`](CLAUDE.md) and [`.claude/rules/`](.claude/rules/) apply to every contributor, with or without AI tools; CLAUDE.md's section "Contributors without write access" lists the steps that differ for you.

- **`OpenUrzednikResult` / `OpenUrzednikResult<T>` for every error that can happen during normal operation** (validation, HTTP statuses, bad responses, network errors, timeouts), instead of error handling scattered across the code. We throw exceptions only when the caller cancels, for programmer errors (a `null` argument or an undefined enum value, which a public method that returns a task throws before it returns the task) and for fatal errors; a value of the right type that fails a format or range check is a returned `ValidationError`. Don't use `catch (Exception)` ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)). Exceptions (`OpenUrzednikException` and its subtypes) are available through `.EnsureSuccess()` / `.EnsureSuccessAsync()` for consumers who prefer the classic style; don't add parallel API variants.
- **Dependencies:** the Core and provider packages have no NuGet dependencies (apart from official BCL packages for netstandard2.0, and SharpCompress in the White List flat-file package); integrations with `Microsoft.Extensions.*` go into separate packages ([ADR-0024](docs/adr/0024-dependency-policy-with-a-7z-reader.md)).
- **Package sources:** the root `nuget.config` restores every package from nuget.org only and maps all of them to it ([Package Source Mapping](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping)), so other feeds in your machine's `NuGet.Config` aren't used in this repository. The mapping applies only to downloads: a package already in your global packages folder is reused as is, so if another project on your machine restores the same packages from another feed, clear that folder (`dotnet nuget locals global-packages --clear`) before you build here. The mapping doesn't apply to commands that look up package metadata (`dotnet package add`, `dotnet list package --outdated`): one you give an extra `--source` queries that source too.
- **`OpenUrzednikError`** is the base type for expected errors. A new error is a `sealed` class ([ADR-0011](docs/adr/0011-compatibility-policy.md)) that derives from it ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)), defines `public const string ErrorCode`, passes it to the base constructor and overrides `CreateException()` to return an exception derived from `OpenUrzednikException` (see `NotFoundError`). A provider's subclass that adds details of its API to an existing condition (`WhiteListBadRequestError : HttpBadRequestError`) is the exception: it keeps its condition's `Code` and exception ([ADR-0021](docs/adr/0021-transport-subclasses-of-condition-errors.md)).
- **Provider packages** follow the patterns in `OpenUrzednik.Core`: type names, error naming and the way results are returned stay consistent.
- **Document the public API** with `///` comments. `GenerateDocumentationFile` is on, so they show up in the consumer's IntelliSense.
- **Tests** must not depend on real government servers in the default CI run. Test HTTP scenarios with stubs (`StubHttpMessageHandler`) and WireMock, using responses captured from the real API (if they can contain personal data, capture them from a test environment or from legal-person records and replace remaining personal fields with fake values of the same format; never commit API keys, session ids or tokens). Mark tests that call the real API with `[ManualFact]` / `[ManualTheory]`. They run only on request: `dotnet test tests/OpenUrzednik.IntegrationTests --explicit on`, or with the `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` variable set.

## Reporting security vulnerabilities

See [SECURITY.md](SECURITY.md). Don't report vulnerabilities in a public issue.

## Pull requests

1. Fork, then branch from `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`).
2. Target `develop`; we merge PRs with a merge commit. The `main` branch takes only release PRs (from `release/X.Y` branches) and hotfixes (see [`docs/RELEASE-PROCESS.md`](docs/RELEASE-PROCESS.md)). A bug fix targets `develop` too: the maintainer decides at triage whether it also ships as a hotfix of the current version, and takes care of that part. Report a vulnerability privately ([SECURITY.md](SECURITY.md)), never in an issue or a PR.
3. Before opening a PR, run `dotnet build OpenUrzednik.slnx -p:TreatWarningsAsErrors=true` (CI fails on any warning), `dotnet test OpenUrzednik.slnx -f net10.0` and `dotnet format OpenUrzednik.slnx --verify-no-changes` locally. The tests run on Microsoft.Testing.Platform, so filters are xUnit's, not VSTest's `--filter`: `dotnet test tests/OpenUrzednik.Nbp.Tests --filter-class "*NbpGoldPriceClientTest"`.
4. If you change code in a file the mutation tests treat as strict (`tests/Mutation/shards.json`), run that shard too: `dotnet tool restore` once, then `uv run tests/Mutation/run.py <shard>` (or `python tests/Mutation/run.py <shard>`). It runs [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/), which changes your code in small ways and checks that a test fails for each change; the report in `artifacts/mutation/<shard>/reports/` lists the changes no test noticed. Your PR runs the shards its change affects (`uv run tests/Mutation/run.py --changed-since upstream/develop --all` picks the same ones locally from your committed changes), and every release PR runs all of them ([ADR-0020](docs/adr/0020-mutation-testing-of-every-target-branch.md)), so a gap found now doesn't fail your PR or block a release later.
5. If you add, change or remove a public member of a package, the build fails (RS0016/RS0017) until the package's `PublicAPI.Unshipped.txt` lists it (your IDE's fix for RS0016 writes the line; a removal is a `*REMOVED*` line). The diff of that file shows reviewers the change's whole public API; don't edit `PublicAPI.Shipped.txt`, which only a release changes.
6. If you change a script (`.github/scripts`, `.claude/hooks`, the `/prepare-release` scripts, `tests/Mutation/run.py`) or a workflow step with logic (branches, parsing, loops), add its cases to `tests/Scripts/edge-tests.sh` and run it ([below](#testing-scripts-and-workflow-steps)). CI runs it on PRs that touch those files.
7. Describe *what* and *why*, not only *how*, especially for changes in `Core`. PR descriptions and commit messages are in English ([ADR-0009](docs/adr/0009-documentation-language.md)).
8. The docs are in English. Next to the root README and the README of each published package sits a short Polish `README.pl.md` with only the informational parts: what the package is and the "unofficial" notice, installation, the quick-start example, the root package table and links to the English sections. If you change one of those parts and don't write Polish, say so in the PR; the maintainer updates the Polish file.

## Testing scripts and workflow steps

`tests/Scripts/edge-tests.sh` tests the repository's scripts and the workflow steps with logic: the format hook, the release, notes and label checks, the `test (net472)` loop, the `/prepare-release` scripts, the mutation workflow's shard selection, summary and aggregate check, and `package_guard.py`. Each case takes its step from the workflow file in your checkout and runs it the way an Ubuntu runner does (`tests/Scripts/run-step.sh`), in a temp directory; nothing in the repository changes.

```bash
bash tests/Scripts/edge-tests.sh
```

It prints one line per case and exits 1 if one fails. A known, accepted failure prints `KNOWN` and doesn't count; the list and the reasons are at the top of the script.

A new case covers the normal input first, then the edge cases: empty or missing input, unusual names and characters (spaces, Polish letters), re-runs, and a failing tool the step calls. A single command with no logic (`dotnet build …`) needs none.

**Linux** (as CI, `ubuntu-latest`): bash 3.2 or later, git, jq, [yq by Mike Farah](https://github.com/mikefarah/yq#install) (not the Python `yq`, which takes other arguments), python3, [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/install-ubuntu) and the [.NET 10 SDK](https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install#ubuntu-2404). On Ubuntu 24.04: `sudo apt-get install -y git jq python3 dotnet-sdk-10.0`, then yq and PowerShell from the linked pages.

**Windows:** through WSL 2, not Git Bash (the cases need Linux tools and file names). Install a distribution once with `wsl --install -d Ubuntu-24.04`, install the Linux tools above inside it, then run the script on your checkout through its `/mnt/c/…` path:

```powershell
wsl -d Ubuntu-24.04 -- bash /mnt/c/<path to your clone>/tests/Scripts/edge-tests.sh
```

A checkout made by `git worktree` on Windows works too: the cases don't run git in your checkout.

**macOS:** works with the system bash (3.2): `brew install jq yq python` ([Homebrew](https://brew.sh); its `yq` is Mike Farah's), plus [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/install-powershell-on-macos) and the [.NET 10 SDK](https://learn.microsoft.com/dotnet/core/install/macos). The file system ignores case, so the case that needs a case-sensitive one (C5) prints `SKIP`. Checked on GitHub's `macos-latest` runner.

## Working with AI agents

Instructions for agents (Claude Code, Copilot and others) are in [`CLAUDE.md`](CLAUDE.md), and the rest of each area's rules are in [`.claude/rules/`](.claude/rules/). [`AGENTS.md`](AGENTS.md) points to both and lists which rule file applies to which files.
