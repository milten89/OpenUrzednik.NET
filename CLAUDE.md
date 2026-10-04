# CLAUDE.md

Instructions for AI coding agents working in this repository. Also read [`docs/adr/`](docs/adr/README.md): accepted ADRs are binding, and this file summarizes them.

## Project

OpenUrzednik.NET is a set of unofficial .NET client libraries for Polish public APIs, published to NuGet as separate packages.

| Project | State |
|---|---|
| `src/OpenUrzednik.Core` | Result pattern (`OpenUrzednikResult`, `OpenUrzednikResult<T>`), errors and exceptions, telemetry abstractions, `ValueValidator<T>` |
| `src/OpenUrzednik.Http` | Shared REST/JSON layer for providers: `RestRequestExecutor`, `RestProviderProfile`, `OpenUrzednikTelemetry` (namespace `OpenUrzednik.Http.Infrastructure`) |
| `src/OpenUrzednik.Extensions.Logging` | `IOpenUrzednikLogger` over `ILogger` (`OpenUrzednikLogger`, `CreateOpenUrzednikLogger<TClient>()`) |
| `src/OpenUrzednik.Diagnostics` | `IOpenUrzednikTraceSource` over `ActivitySource` (`ActivityTraceSource`, `GetShared("OpenUrzednik.<Provider>")`) |
| `src/OpenUrzednik.Nbp` | NBP API: currency rates (`Currency/`), rate tables (`Table/`), gold prices (`Gold/`). **Reference provider**, still being hardened |
| `src/OpenUrzednik.Nbp.DependencyInjection` | `services.AddOpenUrzednikNbp()`: the NBP clients as typed clients of one named `HttpClient`, validated options, adapters wired; returns the `IHttpClientBuilder` |
| `src/OpenUrzednik.Gus`, `Krs`, `Mf` | Empty skeletons. **Do not work on them** ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)) |
| `tests/OpenUrzednik.*.Tests` | Unit tests (xUnit v3, Shouldly, NSubstitute, Bogus, `FakeTimeProvider`) |
| `tests/OpenUrzednik.IntegrationTests` | WireMock tests (run in CI) and tests against the real API (explicit: they run with `--explicit on` or when `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` is set) |
| `tests/OpenUrzednik.TestCommon` | Shared test helpers: `StubHttpMessageHandler`, `ManualFact`/`ManualTheory`, Faker extensions |

Current priorities are listed in [`docs/BACKLOG.md`](docs/BACKLOG.md). Pick work from there unless told otherwise.

## Commands

```bash
dotnet build OpenUrzednik.slnx
dotnet test OpenUrzednik.slnx                    # all TFMs; locally only installed runtimes work
dotnet test OpenUrzednik.slnx -f net10.0         # fastest local loop
dotnet test tests/OpenUrzednik.Nbp.Tests --filter-class "*NbpGoldPriceClientTest"
dotnet format OpenUrzednik.slnx --verify-no-changes   # CI fails if this reports changes
dotnet test tests/OpenUrzednik.IntegrationTests --explicit on   # also hits the real NBP API
```

Before you say a task is done, run `dotnet build`, `dotnet test -f net10.0` and `dotnet format --verify-no-changes`, and report the results.

## Architecture rules (from ADRs)

**Errors ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md))**
- Public async APIs return `Task<OpenUrzednikResult<T>>`. Failures that can happen during normal execution are returned, not thrown: validation, every non-success HTTP status, bad payloads, network errors, timeouts.
- Throw only for caller cancellation (`OperationCanceledException` when the caller's token is cancelled), programmer errors (`ArgumentNullException`, invalid options) and fatal errors.
- **Never write `catch (Exception)` or a bare `catch`.** Catch specific types and convert them to errors.
- A timeout is an `OperationCanceledException` while `cancellationToken.IsCancellationRequested == false`. Return it as an error.
- New error types derive from `OpenUrzednikError`, define `public const string ErrorCode`, and implement `CreateException()` returning an `OpenUrzednikException` subtype. The public `ToException()` calls it and attaches the error (`OpenUrzednikException.Error`/`Errors`).
- Several errors (only validation produces them) are thrown by `EnsureSuccess()` as one `ValidationException` listing all of them, never as `AggregateException`. Several errors of other kinds: the first error's exception, with all of them in `Errors`.

**Dependencies ([ADR-0004](docs/adr/0004-dependency-policy.md))**
- Core, `OpenUrzednik.Http` and provider packages take **no package dependencies** on .NET targets.
- On netstandard2.0, only official Microsoft BCL packages are allowed (`System.Text.Json`, `System.Net.Http.Json`, `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode`, `System.Diagnostics.DiagnosticSource`), referenced in a `Condition="'$(TargetFramework)' == 'netstandard2.0'"` item group.
- `Microsoft.Extensions.*` is allowed only in integration packages (`*.DependencyInjection`, `OpenUrzednik.Extensions.Logging`). The tracing adapter `OpenUrzednik.Diagnostics` may depend only on `System.Diagnostics.DiagnosticSource`, and only on netstandard2.0.
- DI packages don't add a resilience handler: they return the `IHttpClientBuilder`, the app chains `AddStandardResilienceHandler()`, and the DI package turns the handler's rejections into errors.
- Versions live in `Directory.Packages.props` (central package management). Never put `Version=` on a `PackageReference`.
- Version choice for packages the libraries depend on: **abstractions** (API contracts under semantic versioning, e.g. `Microsoft.Extensions.Logging.Abstractions`) use the **lowest** version: the first release (`x.0.0`) of the major that matches our oldest .NET target (currently 8.0.0), because it is a minimum for every app. Move to a later patch only if that version has a known vulnerability; the minimum rises when the oldest .NET target is removed (ADR-0005). **Implementations** (code that runs, e.g. `System.Text.Json`, `Microsoft.Extensions.Http`) use the **latest** version, to get bug and security fixes. `Directory.Packages.props` and the Dependabot groups (`abstractions` has its own) follow the split. Accepted consequence: the DI package brings `Microsoft.Extensions.*` 10.x into .NET 8 apps through `Microsoft.Extensions.Http`.
- **If a change raises an abstraction above its minimum** (typically a Dependabot PR), stop and ask the maintainer explicitly what to do, naming the package, both versions and the rule. Don't merge, approve, rebase onto or build on that change until they answer explicitly.
- Do not add a `PackageReference` to a `src/` project without an ADR or explicit approval.

**Telemetry ([ADR-0003](docs/adr/0003-telemetry-abstractions.md))**
- Use `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` from Core, with `Null*` defaults. Don't use `ILogger` or `ActivitySource` in Core or provider packages.
- Span names: `<provider>.<area>.<operation>` (e.g. `nbp.currency.buy_sell_latest`). Tags: `<provider>.<parameter>`; HTTP tags follow OpenTelemetry (`http.request.method`, `url.path`, `http.response.status_code`). Record failures with `span.RecordError(s)`.
- Adapters: `OpenUrzednik.Extensions.Logging` (logger category = the client's full type name) and `OpenUrzednik.Diagnostics` (source `OpenUrzednik.<Provider>`, e.g. `NbpTelemetry.SourceName`; every span is `ActivityKind.Internal`).
- Guard `Debug` logs with `IsEnabled`, use message templates (no interpolation), and never log secrets or full personal identifiers.

**Target frameworks ([ADR-0005](docs/adr/0005-target-frameworks.md))**
- Libraries target `netstandard2.0;net8.0;net9.0;net10.0` (`src/Directory.Build.props`); tests add `net472`, which runs the netstandard2.0 build on .NET Framework (CI job `test (net472)`; xUnit 4 supports nothing older). A .NET target becomes removable 6 months after Microsoft ends its support and is removed in the next major release; netstandard2.0 stays.
- Dates are `DateOnly` on .NET and `DateTime` on netstandard2.0. Write `DateOnly`: on netstandard2.0 it is a global alias for `DateTime` (`OpenUrzednik.Nbp.csproj`, `tests/Directory.Build.props`), and `Nbp/Polyfills/DateOnlyPolyfills.cs` adds the `DateOnly` members the code uses (`FromDateTime`, `DayNumber`). Compare dates by `DayNumber`, which ignores a `DateTime`'s time of day.
- Missing BCL members are polyfilled for netstandard2.0 only: compiler and nullable attributes, the `ThrowIf*` guards, `Enum.IsDefined<T>`, `string.Create(IFormatProvider, …)` and `string.EndsWith(char)` (C# 14 extension members) in `src/Polyfills`, linked into every library; HTTP reads with a cancellation token in `Http/Polyfills`; span `StringBuilder.Append`/`string.Concat` in `Extensions.Logging/Polyfills`. Where a polyfill would clash with a library method of the same signature, a small helper holds the `#if` (`Http/Infrastructure/HttpContentJson.cs`). Keep `#if` inside such helpers, not spread through business logic.
- .NET Framework differences seen so far: `HttpResponseMessage.Content` can be `null`; `HttpClient.Timeout` has no inner `TimeoutException`; time zones have Windows ids only (`Central European Standard Time`); `Activity` ids are hierarchical unless the app (or OpenTelemetry) switches to W3C.

**HTTP ([ADR-0006](docs/adr/0006-shared-http-layer.md))**
- All provider HTTP goes through `RestRequestExecutor.GetAsync` in `OpenUrzednik.Http`; providers never call `HttpClient.SendAsync`. A provider describes itself with a `RestProviderProfile` (span name `<name>.http.get`, message prefix, optional `MapErrorAsync` override). NBP creates its executor in `Nbp/Common/NbpConnection.cs`. Clients take an `HttpClient` and optional `NbpOptions`; they never change the `HttpClient`.
- Dispose `HttpRequestMessage` and `HttpResponseMessage`.
- Deserialize with source-generated `JsonTypeInfo<T>` (`NbpJsonContext`); no reflection-based serialization.

**Client API ([ADR-0007](docs/adr/0007-client-api-and-extensibility.md))**
- The default case is one line (`new NbpGoldPriceClient(httpClient)`).
- Parts that shape requests (e.g. `INbpUrlBuilderFactory`) are public interfaces with public defaults, passed as optional constructor parameters.
- Cross-cutting HTTP concerns belong in `DelegatingHandler`s.
- `CancellationToken cancellationToken = default` is the last parameter of every async method, in both interfaces and implementations.

## Performance

- **Strongly prefer the faster code over the more convenient one** in library code (`src/`). Readability matters, but not at the cost of performance on a path that runs per request, per log entry or per item: use spans instead of `Substring`, avoid per-call allocations (arrays built from constants, closures, boxing, LINQ, string concatenation), and prefer generic APIs to reflection.
- **Supporting an older target must not slow down the .NET targets.** Hide the difference in a **polyfill** when possible: a member with the .NET name, compiled only for the target that lacks it (`src/Polyfills`, `Http/Polyfills`, `Nbp/Polyfills`), so the call site is the same everywhere. Only when no polyfill fits, use `#if NET` in the code, around as little code as possible. Never replace the fast .NET code with a slower shared version.
- The performance analyzers (all CA18xx rules) are build warnings in `src/` (`AnalysisModePerformance=All` in `src/Directory.Build.props`), and in CI they fail the build (`CodeAnalysisTreatWarningsAsErrors`). Fix the warning. Suppress it only with `[SuppressMessage(..., Justification = "...")]` saying why the analyzer is wrong in that place.
- Hot paths, where this matters most: `RestRequestExecutor`, the NBP pipeline and mappers, `LogValues`/`OpenUrzednikLogger`, `ActivitySpan`, `OpenUrzednikResult`.
- The `reviewer` agent reports performance degradations in a section of their own; treat each like any other finding. The rules come from [ADR-0005](docs/adr/0005-target-frameworks.md) (target differences) and [ADR-0004](docs/adr/0004-dependency-policy.md) (versions).

## Code conventions

- C# `latest`, nullable enabled, file-scoped namespaces, `_camelCase` private fields. `.editorconfig` is authoritative.
- Public models are `sealed record`s with `IReadOnlyList<T>` collections. Records holding lists override `Equals`/`GetHashCode` (see `Currency/CurrencyExchangeRates.cs`).
- DTOs are `internal sealed class` in `Dto/`, with `[JsonPropertyName]`. Mark a property `required` **only if the real API always returns it**: check with `/verify-api`.
- Mappers are `internal static class Mapper` per area. Validators derive from `ValueValidator<T>` in `Validation/` and are `internal sealed`.
- Public API needs `///` XML docs. Mention API limits (date ranges, top count, publication schedule).
- Large clients are split into `partial` files by area (`NbpCurrencyExchangeRateClient.BuySell.cs`).
- Messages and logs are in English. Format dates invariantly (`yyyy-MM-dd`).

## Testing conventions

- One `partial` test class per production class, with one file per method: `NbpGoldPriceClientTest.GetLatestAsync.cs`. Shared helpers go in the main file (`NbpGoldPriceClientTest.cs`).
- Test names: `Method_Condition_ExpectedResult`. Use `// Arrange / // Act / // Assert` sections.
- Use deterministic data: `new Faker().WithConstantSeed()`, the DTO fakers in `tests/OpenUrzednik.Nbp.Tests/Fakes`, and `FakeTimeProvider`.
- Fake HTTP in unit tests with `StubHttpMessageHandler`; mock interfaces with NSubstitute. Pass `TestContext.Current.CancellationToken` to async calls.
- WireMock tests must use **response bodies captured from the real API**, not hand-written JSON. Each error path in ADR-0002 (404, 400, 429 with `Retry-After`, 5xx, timeout, connection failure, malformed JSON) needs a test.
- Tests against the real API use `[ManualFact]`/`[ManualTheory]` (explicit tests) and must never run in default CI.
- Tests run on Microsoft.Testing.Platform (`global.json`), so filters are xUnit's (`--filter-class`, `--filter-method`, `--filter-trait`), not VSTest's `--filter`. Filter one project: on the whole solution, projects with no matching test make the run exit with code 8 (add `--ignore-exit-code 8`).

## Workflow

- Branch from `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`). PRs target `develop` and are squash-merged. Releases go `develop` → `main` ([ADR-0008](docs/adr/0008-branching-versioning-and-release.md)).
- Commit or push only when asked. Keep one backlog item per PR.
- If a change contradicts an accepted ADR, stop and use `/adr` instead of working around it. Until the first stable release, accepted ADRs are changed in place with a dated note (ADR-0001); from 1.0 on, a new superseding ADR.
- Before opening a PR, run the `reviewer` agent on the diff.
- **CI job names are required status checks in the GitHub ruleset.** If you rename a job in `.github/workflows/build.yml` or `format.yml`, say so: the ruleset must be updated (see `docs/GITHUB-SETUP.md`).
- Language ([ADR-0009](docs/adr/0009-documentation-language.md)): code, ADRs and technical docs are in English. User-facing docs (README, CONTRIBUTING) are in Polish and link to an English version. Update both versions together.
- Prose style: when you write or edit a README (root or per package), run `/miodkuj` on the Polish version (`README.md`) and `/stop-slop` on the English version (`README.en.md`). Use the same skills for `CONTRIBUTING`, `SECURITY` and other user-facing prose. Edit only the prose: keep code blocks, commands, package and API names, badges, links and numbers exactly as they are.

## Skills and agents

- `/new-endpoint`: checklist for adding or changing a client method (DTO, mapper, validation, client, tests, docs).
- `/verify-api`: compare DTOs and test payloads with live API responses, and capture fixtures.
- `/adr`: create a new MADR record and update the index.
- `/miodkuj`: remove AI-sounding, bureaucratic and translated phrasing from Polish prose (vendored from [bartekpucek/miodkuj](https://github.com/bartekpucek/miodkuj), MIT).
- `/stop-slop`: remove predictable AI writing patterns from English prose (vendored from [hardikpandya/stop-slop](https://github.com/hardikpandya/stop-slop), MIT).
- `reviewer` agent: reviews a diff against the ADRs and the conventions above.
