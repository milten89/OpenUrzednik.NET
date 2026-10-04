# Contributing to OpenUrzednik.NET

🇵🇱 [Wersja polska](CONTRIBUTING.md)

## Before you start

- Check the [issues](../../issues): someone may already be working on it.
- For larger changes (a new package, a change to the public API), open an issue with a proposal before you write code. It saves time on both sides.

## Repository conventions

Binding architecture decisions are in [`docs/adr/`](docs/adr/README.md), and current priorities are in [`docs/BACKLOG.md`](docs/BACKLOG.md). A change that contradicts an accepted ADR needs a new ADR in the same PR.

- **`OpenUrzednikResult` / `OpenUrzednikResult<T>` for every error that can happen during normal operation** (validation, HTTP statuses, bad responses, network errors, timeouts), instead of error handling scattered across the code. We throw exceptions only when the caller cancels, for programmer errors (e.g. a `null` argument) and for fatal errors. Don't use `catch (Exception)` ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)). Exceptions (`OpenUrzednikException` and its subtypes) are available through `.EnsureSuccess()` / `.EnsureSuccessAsync()` for consumers who prefer the classic style; don't add parallel API variants.
- **Dependencies:** the Core and provider packages have no NuGet dependencies (apart from official BCL packages for netstandard2.0); integrations with `Microsoft.Extensions.*` go into separate packages ([ADR-0004](docs/adr/0004-dependency-policy.md)).
- **`OpenUrzednikError`** is the base type for expected errors. Each concrete error derives from it and implements `Code` and `CreateException()`, which returns an exception derived from `OpenUrzednikException`.
- **Provider packages** follow the patterns in `OpenUrzednik.Core`: type names, error naming and the way results are returned stay consistent.
- **Document the public API** with `///` comments. `GenerateDocumentationFile` is on, so they show up in the consumer's IntelliSense.
- **Tests** must not depend on real government servers in the default CI run. Test HTTP scenarios with stubs (`StubHttpMessageHandler`) and WireMock, using responses captured from the real API. Mark tests that call the real API with `[ManualFact]` / `[ManualTheory]`. They run only on request: `dotnet test tests/OpenUrzednik.IntegrationTests --explicit on`, or with the `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` variable set.

## Reporting security vulnerabilities

See [SECURITY.en.md](SECURITY.en.md). Don't report vulnerabilities in a public issue.

## Pull requests

1. Fork, then branch from `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`).
2. Target `develop`; we *squash*-merge changes. The `main` branch takes only release PRs from `develop` and hotfixes (see [`docs/RELEASE-PROCESS.md`](docs/RELEASE-PROCESS.md)).
3. Before opening a PR, run `dotnet test OpenUrzednik.slnx` and `dotnet format OpenUrzednik.slnx --verify-no-changes` locally.
4. Describe *what* and *why*, not only *how*, especially for changes in `Core`. PR descriptions and commit messages are in English ([ADR-0009](docs/adr/0009-documentation-language.md)).

## Working with AI agents

Instructions for agents (Claude Code, Copilot and others) are in [`CLAUDE.md`](CLAUDE.md) / [`AGENTS.md`](AGENTS.md).
