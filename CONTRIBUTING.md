# Contributing to OpenUrzednik.NET

## Prerequisites

- The [.NET 10 SDK](https://dotnet.microsoft.com/download) or later, and the .NET 8 runtime to run the `net8.0` tests.
- For the `net472` tests: Windows with .NET Framework 4.7.2 or later (CI runs them on 4.8). Building needs no .NET Framework targeting pack; the SDK brings the reference assemblies.

## Before you start

- Check the [issues](https://github.com/milten89/OpenUrzednik.NET/issues) and the [project](https://github.com/users/milten89/projects/2): someone may already be working on it. Comment on the issue before you start.
- For larger changes (a new package, a change to the public API), open an issue with a proposal before you write code. It saves time on both sides.
- New providers wait: work on GUS, KRS, MF or any other data source begins only after the first stable release of `OpenUrzednik.Nbp` ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)). We close PRs that change the `src/OpenUrzednik.Gus`, `Krs` or `Mf` skeletons; propose a provider with the provider proposal issue form instead.

## Repository conventions

Binding architecture decisions are in [`docs/adr/`](docs/adr/README.md), and current work and priorities are in the [issues](https://github.com/milten89/OpenUrzednik.NET/issues) and the [project](https://github.com/users/milten89/projects/2). A change that contradicts an accepted ADR needs a new ADR in the same PR. The Code conventions, Testing conventions and Performance sections of [`CLAUDE.md`](CLAUDE.md) apply to every contributor; using AI tools is optional.

- **`OpenUrzednikResult` / `OpenUrzednikResult<T>` for every error that can happen during normal operation** (validation, HTTP statuses, bad responses, network errors, timeouts), instead of error handling scattered across the code. We throw exceptions only when the caller cancels, for programmer errors (a `null` argument or an undefined enum value, which a client method throws before it returns its task) and for fatal errors; a value of the right type that fails a format or range check is a returned `ValidationError`. Don't use `catch (Exception)` ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)). Exceptions (`OpenUrzednikException` and its subtypes) are available through `.EnsureSuccess()` / `.EnsureSuccessAsync()` for consumers who prefer the classic style; don't add parallel API variants.
- **Dependencies:** the Core and provider packages have no NuGet dependencies (apart from official BCL packages for netstandard2.0); integrations with `Microsoft.Extensions.*` go into separate packages ([ADR-0004](docs/adr/0004-dependency-policy.md)).
- **`OpenUrzednikError`** is the base type for expected errors. A new error is a `sealed` class ([ADR-0011](docs/adr/0011-compatibility-policy.md)) that derives from it ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)), defines `public const string ErrorCode`, passes it to the base constructor and overrides `CreateException()` to return an exception derived from `OpenUrzednikException` (see `NotFoundError`).
- **Provider packages** follow the patterns in `OpenUrzednik.Core`: type names, error naming and the way results are returned stay consistent.
- **Document the public API** with `///` comments. `GenerateDocumentationFile` is on, so they show up in the consumer's IntelliSense.
- **Tests** must not depend on real government servers in the default CI run. Test HTTP scenarios with stubs (`StubHttpMessageHandler`) and WireMock, using responses captured from the real API (if they can contain personal data, capture them from a test environment or from legal-person records and replace remaining personal fields with fake values of the same format; never commit API keys, session ids or tokens). Mark tests that call the real API with `[ManualFact]` / `[ManualTheory]`. They run only on request: `dotnet test tests/OpenUrzednik.IntegrationTests --explicit on`, or with the `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` variable set.

## Reporting security vulnerabilities

See [SECURITY.md](SECURITY.md). Don't report vulnerabilities in a public issue.

## Pull requests

1. Fork, then branch from `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`).
2. Target `develop`; we merge PRs with a merge commit. The `main` branch takes only release PRs (from `release/X.Y` branches) and hotfixes (see [`docs/RELEASE-PROCESS.md`](docs/RELEASE-PROCESS.md)).
3. Before opening a PR, run `dotnet build OpenUrzednik.slnx`, `dotnet test OpenUrzednik.slnx -f net10.0` and `dotnet format OpenUrzednik.slnx --verify-no-changes` locally. The tests run on Microsoft.Testing.Platform, so filters are xUnit's, not VSTest's `--filter`: `dotnet test tests/OpenUrzednik.Nbp.Tests --filter-class "*NbpGoldPriceClientTest"`.
4. Describe *what* and *why*, not only *how*, especially for changes in `Core`. PR descriptions and commit messages are in English ([ADR-0009](docs/adr/0009-documentation-language.md)).
5. The docs are in English. Next to the root README and the README of each published package sits a short Polish `README.pl.md` with only the informational parts: what the package is and the "unofficial" notice, installation, the quick-start example, the root package table and links to the English sections. If you change one of those parts and don't write Polish, say so in the PR; the maintainer updates the Polish file.

## Working with AI agents

Instructions for agents (Claude Code, Copilot and others) are in [`CLAUDE.md`](CLAUDE.md). [`AGENTS.md`](AGENTS.md) only points to it.
