# CLAUDE.md

Instructions for AI coding agents working in this repository. Also read [`docs/adr/`](docs/adr/README.md): accepted ADRs are binding, and this file summarizes them.

## Project

OpenUrzednik.NET is a set of unofficial .NET client libraries for Polish public APIs, published to NuGet as separate packages.

| Project | State |
|---|---|
| `src/OpenUrzednik.Core` | Result pattern (`OpenUrzednikResult`, `OpenUrzednikResult<T>`), errors and exceptions, telemetry abstractions, `ValueValidator<T>` |
| `src/Http` | Shared REST/JSON layer, not a package: internal source compiled into each REST provider through `Http.props` (a DI package links `Infrastructure/ResilienceRejection.cs` to mark resilience rejections) (`RestRequestExecutor`, `RestProviderProfile`, `OpenUrzednikTelemetry`; namespace `OpenUrzednik.Http.Infrastructure`) |
| `src/OpenUrzednik.Extensions.Logging` | `IOpenUrzednikLogger` over `ILogger` (`OpenUrzednikLogger`, `CreateOpenUrzednikLogger<TClient>()`) |
| `src/OpenUrzednik.Diagnostics` | `IOpenUrzednikTraceSource` over `ActivitySource` (`ActivityTraceSource`, `GetShared("OpenUrzednik.<Provider>")`) |
| `src/OpenUrzednik.Nbp` | NBP API: currency rates (`Currency/`), rate tables (`Table/`), gold prices (`Gold/`). **Reference provider**, still being hardened |
| `src/OpenUrzednik.Nbp.DependencyInjection` | `services.AddOpenUrzednikNbp()`: the NBP clients as typed clients of one named `HttpClient`, validated options, adapters wired; returns the `IHttpClientBuilder` |
| `src/OpenUrzednik.Gus`, `Krs`, `Mf` | Empty skeletons. **Do not work on them** ([ADR-0010](docs/adr/0010-provider-readiness-gate.md)) |
| `tests/OpenUrzednik.*.Tests` | Unit tests (xUnit v3, Shouldly, NSubstitute, Bogus, `FakeTimeProvider`) |
| `tests/OpenUrzednik.IntegrationTests` | WireMock tests (run in CI) and tests against the real API (explicit: they run with `--explicit on` or when `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` is set) |
| `tests/OpenUrzednik.TestCommon` | Shared test helpers: `StubHttpMessageHandler`, `ManualFact`/`ManualTheory`, Faker extensions |
| `samples/` | Runnable examples against the real NBP API (console, minimal Web API with DI); built in CI with the solution, not packed |

Work is tracked in GitHub issues and the project [OpenUrzednik.NET](https://github.com/users/milten89/projects/2); see "Issues and the project" below. Pick work from there unless told otherwise. `docs/BACKLOG.md` is a closed record.

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
- Throw only for caller cancellation (`OperationCanceledException` when the caller's token is cancelled), programmer errors (`ArgumentNullException`, invalid options) and fatal errors. In provider clients, a `null` argument and an undefined enum value (`ArgumentOutOfRangeException`) throw **synchronously**: the public method checks them and returns the task of a private `…CoreAsync` method. A non-null value of the right type that fails a format or range check is a returned `ValidationError`.
- **Never write `catch (Exception)` or a bare `catch`.** Catch specific types and convert them to errors.
- A timeout is an `OperationCanceledException` while `cancellationToken.IsCancellationRequested == false`. Return it as an error.
- New error types derive from `OpenUrzednikError`, define `public const string ErrorCode`, and implement `CreateException()` returning an `OpenUrzednikException` subtype. The public `ToException()` calls it and attaches the error (`OpenUrzednikException.Error`/`Errors`).
- Several errors (only validation produces them) are thrown by `EnsureSuccess()` as one `OpenUrzednikValidationException` listing all of them, never as `AggregateException`. Several errors of other kinds: the first error's exception, with all of them in `Errors`.
- Results are not comparable: don't add `Equals`/`==` to `OpenUrzednikResult` (CA1815 stays suppressed); compare `IsSuccess`, the value or error codes.

**Dependencies ([ADR-0004](docs/adr/0004-dependency-policy.md))**
- Core and provider packages take **no package dependencies** on .NET targets.
- On netstandard2.0, only official Microsoft BCL packages are allowed (`System.Text.Json`, `System.Net.Http.Json`, `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode`), referenced in a `Condition="'$(TargetFramework)' == 'netstandard2.0'"` item group.
- `Microsoft.Extensions.*` is allowed only in integration packages (`*.DependencyInjection`, `OpenUrzednik.Extensions.Logging`). The tracing adapter `OpenUrzednik.Diagnostics` may depend only on `System.Diagnostics.DiagnosticSource`, and only on netstandard2.0.
- DI packages don't add a resilience handler: they return the `IHttpClientBuilder`, the app chains `AddStandardResilienceHandler()`, and the DI package turns the handler's rejections into errors.
- Versions live in `Directory.Packages.props` (central package management). Never put `Version=` on a `PackageReference`.
- Version choice applies to the **dependencies of the published libraries** only; tests, build tooling and anything used only inside the repository take the latest version. For those dependencies: **abstractions** (API contracts under semantic versioning, e.g. `Microsoft.Extensions.Logging.Abstractions`) use the **lowest** version: the first release (`x.0.0`) of the major that matches our oldest .NET target (currently 8.0.0), because it is a minimum for every app. Move to a later patch only if that version has a known vulnerability; the minimum rises when the oldest .NET target is removed (ADR-0005). **Implementations** (code that runs, e.g. `System.Text.Json`, `Microsoft.Extensions.Http`) use the **latest patch** of their chosen minor, to get bug and security fixes; a minor or major raise is a deliberate change. `Directory.Packages.props` follows the split, and Dependabot ignores version updates of `*.Abstractions`, so watch Dependabot alerts for them. The DI package's `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options` take the latest patch of the major matching the target: a normal row with 10.0.x (`net10.0`, `netstandard2.0`) and a `net8.0`-only `PackageVersion Update` override with 8.0.x, so a .NET 8 app stays on `Microsoft.Extensions.*` 8.x. Don't put two conditional `Include` rows for one package: Dependabot skips such packages. Dependabot proposes only patch updates for the published libraries' dependencies, in one group (listed in `.github/dependabot.yml`); the `net8.0` overrides are patched by hand.
- **If a change raises an abstraction above its minimum** (typically a Dependabot PR), stop and ask the maintainer explicitly what to do, naming the package, both versions and the rule. Don't merge, approve, rebase onto or build on that change until they answer explicitly.
- Do not add a `PackageReference` to a `src/` project without an ADR or explicit approval.

**Telemetry ([ADR-0003](docs/adr/0003-telemetry-abstractions.md))**
- Use `IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikSpan` from Core. Don't use `ILogger` or `ActivitySource` in Core or provider packages. The one exception is the fallback trace source: on the .NET targets a client without a trace source uses the shared `OpenUrzednik.<Provider>` source, through Core's internal `Telemetry/Activities` (compiled into Core on .NET only, linked into `OpenUrzednik.Diagnostics` for netstandard2.0; Core grants `InternalsVisibleTo` on .NET only, and a new REST provider must be added to it). netstandard2.0 falls back to `NullOpenUrzednikTraceSource`. Those Core internals are called across packages, and an app may pair a newer Core with older packages: within a major, add to them, never change or remove.
- These interfaces are frozen from 1.0: a new capability is a separate interface that callers check with `is`.
- Span names: `<provider>.<area>.<operation>` (e.g. `nbp.currency.buy_sell_latest`). Tags: `<provider>.<parameter>`; HTTP tags follow OpenTelemetry (`http.request.method`, `url.path`, `http.response.status_code`). Record failures with `span.RecordError(s)`, which set OpenTelemetry's `error.type` to the error code; the `error` events are best-effort.
- Every log call passes an `OpenUrzednikEventId`. The shared HTTP layer's events are in `src/Http/Infrastructure/RestLogEvents.cs` (1xxx); a provider's own are 2xxx and listed, with the HTTP ones, as `public const int` in its `<Provider>LogEvents` class (`NbpLogEvents`; the event name is the constant's name). A new log event gets a new id: never renumber or reuse one. The `{provider}` log property is the profile's short name (`nbp`), as in span names.
- Stable within a major: source names, span names, the documented tags, `error.type`, logger categories and event ids. Log message text and properties aren't.
- Adapters: `OpenUrzednik.Extensions.Logging` (logger category = the client's full type name; the event id becomes `EventId`) and `OpenUrzednik.Diagnostics` (source `OpenUrzednik.<Provider>`, e.g. `NbpTelemetry.SourceName`; every span is `ActivityKind.Internal`).
- Guard `Debug` logs with `IsEnabled`, use message templates (no interpolation), and never log secrets or full personal identifiers.

**Target frameworks ([ADR-0005](docs/adr/0005-target-frameworks.md))**
- Libraries target `netstandard2.0` plus the .NET LTS releases in support, today `netstandard2.0;net8.0;net10.0` (the .NET targets are in the root `Directory.Build.props`); .NET Framework 4.7.2 or later uses netstandard2.0; tests add `net472`, which runs the netstandard2.0 build on .NET Framework (CI job `test (net472)`; xUnit 4 supports nothing older). A .NET target that isn't the lowest one may be removed in a minor version once Microsoft ends its support; the lowest .NET target and netstandard2.0 are removed only in a major.
- Every build has the same public signatures for what it shares; the .NET builds may add members, never change them. Public dates are `DateTime` on every target (parameters and model properties): a method converts its arguments with `DateOnly.FromDateTime` at the start (time of day and `Kind` ignored), and mappers return `ToPublicDate()` (midnight, `DateTimeKind.Unspecified`). `DateOnly` overloads exist only as .NET-only extension methods (`NbpDateOnlyExtensions`), never as interface members.
- Internally, write `DateOnly`: on netstandard2.0 it is a global alias for `DateTime` (`OpenUrzednik.Nbp.csproj`, `tests/Directory.Build.props`), and `Nbp/Polyfills/DateOnlyPolyfills.cs` adds the `DateOnly` members the code uses (`FromDateTime`, `DayNumber`). Compare dates by `DayNumber`, which ignores a `DateTime`'s time of day.
- Missing BCL members are polyfilled for netstandard2.0 only: compiler and nullable attributes, the `ThrowIf*` guards, `Enum.IsDefined<T>`, `string.Create(IFormatProvider, …)` and `string.EndsWith(char)` (C# 14 extension members) in `src/Polyfills`, linked into every library; HTTP reads with a cancellation token in `src/Http/Polyfills`; span `StringBuilder.Append`/`string.Concat` in `Extensions.Logging/Polyfills`. Where a polyfill would clash with a library method of the same signature, a small helper holds the `#if` (`src/Http/Infrastructure/HttpContentJson.cs`). Keep `#if` inside such helpers, not spread through business logic.
- .NET Framework differences seen so far: `HttpResponseMessage.Content` can be `null`; `HttpClient.Timeout` has no inner `TimeoutException`; time zones have Windows ids only (`Central European Standard Time`); `Activity` ids are hierarchical unless the app (or OpenTelemetry) switches to W3C.

**HTTP ([ADR-0006](docs/adr/0006-shared-http-layer.md))**
- All provider HTTP goes through `RestRequestExecutor.GetAsync` in the shared HTTP layer (`src/Http`); providers never call `HttpClient.SendAsync`. A REST provider imports `src/Http/Http.props`, and its types stay `internal`: no public signature uses them. A provider describes itself with a `RestProviderProfile` (span name `<name>.http.get`, message prefix, optional `MapErrorAsync` override). NBP creates its executor in `Nbp/Common/NbpConnection.cs`. Clients take an `HttpClient` and optional `NbpOptions`; they never change the `HttpClient`.
- Dispose `HttpRequestMessage` and `HttpResponseMessage`.
- Deserialize with source-generated `JsonTypeInfo<T>` (`NbpJsonContext`); no reflection-based serialization.

**Compatibility ([ADR-0011](docs/adr/0011-compatibility-policy.md))**
- Now: public classes not designed for inheritance are `sealed` (unsealing is a minor change, sealing after 1.0 a major one). The abstract extension points (`OpenUrzednikError`, `OpenUrzednikException`, `ValueValidator<T>`) never get a new abstract member, and no interface except a client interface gets a new member (telemetry interfaces are frozen).
- From the first stable release:
  - Covered within a major: the public API on every target, documented behaviour, the error contract (ADR-0002) and the telemetry contract (ADR-0003). Not covered: log and exception message text, log properties, undocumented behaviour, internal types (except Core internals called through `InternalsVisibleTo`, which are only added to), previews.
  - A client interface may gain a method in a minor version; package validation reports it (CP0006), and the PR suppresses exactly that diagnostic, citing ADR-0011.
  - Before a major removes a member, mark it `[Obsolete]` (with the replacement) for at least one minor. A patch (`hotfix/*`) adds no public API.
  - A PR with a breaking change carries `breaking-change` and raises `version.json` on `develop` to the next major ([ADR-0008](docs/adr/0008-branching-versioning-and-release.md)).

**Client API ([ADR-0007](docs/adr/0007-client-api-and-extensibility.md))**
- The default case is one line (`new NbpGoldPriceClient(httpClient)`).
- Parts that shape requests (the URL builders, `NbpTable`) are internal; tests replace them through an internal constructor. Each client has two public constructors: `(HttpClient)` and `(HttpClient, NbpOptions?, TimeProvider?, logger, traceSource)`. Gateways use `NbpOptions.ApiUrl` and a `DelegatingHandler`.
- Cross-cutting HTTP concerns belong in `DelegatingHandler`s.
- `CancellationToken cancellationToken = default` is the last parameter of every async method, in both interfaces and implementations.

## Performance

- **Strongly prefer the faster code over the more convenient one** in library code (`src/`). Readability matters, but not at the cost of performance on a path that runs per request, per log entry or per item: use spans instead of `Substring`, avoid per-call allocations (arrays built from constants, closures, boxing, LINQ, string concatenation), and prefer generic APIs to reflection.
- **Supporting an older target must not slow down the .NET targets.** Hide the difference in a **polyfill** when possible: a member with the .NET name, compiled only for the target that lacks it (`src/Polyfills`, `src/Http/Polyfills`, `Nbp/Polyfills`), so the call site is the same everywhere. Only when no polyfill fits, use `#if NET` in the code, around as little code as possible. Never replace the fast .NET code with a slower shared version.
- The performance analyzers (all CA18xx rules) are build warnings in `src/` (`AnalysisModePerformance=All` in `src/Directory.Build.props`), and in CI they fail the build (`CodeAnalysisTreatWarningsAsErrors`). Fix the warning. Suppress it only with `[SuppressMessage(..., Justification = "...")]` saying why the analyzer is wrong in that place.
- Hot paths, where this matters most: `RestRequestExecutor`, the NBP pipeline and mappers, `LogValues`/`OpenUrzednikLogger`, `ActivitySpan`, `OpenUrzednikResult`.
- The `reviewer` agent reports performance degradations in a section of their own; treat each like any other finding. The rules come from [ADR-0005](docs/adr/0005-target-frameworks.md) (target differences) and [ADR-0004](docs/adr/0004-dependency-policy.md) (versions).

## Code conventions

- C# `latest`, nullable enabled, file-scoped namespaces, `_camelCase` private fields. `.editorconfig` is authoritative.
- Namespaces ([ADR-0007](docs/adr/0007-client-api-and-extensibility.md)): clients, their interfaces and options in the provider's root namespace (`OpenUrzednik.Nbp`, files in the project root); models in area namespaces (`.Currency`, `.Table`, `.Gold`). Core: results and their `Task<…>` extensions in `OpenUrzednik.Core`, operations on a result as instance methods, extension methods next to the type they extend. No public `.Extensions` or `.Options` namespaces in Core or providers (package names like `OpenUrzednik.Extensions.Logging` and internal namespaces are exempt).
- Public models are `sealed record`s with `IReadOnlyList<T>` collections. Records holding lists override `Equals`/`GetHashCode` (see `Currency/CurrencyExchangeRates.cs`). New data on an existing model is a non-`required` `init` property, never a new positional parameter (it would change the constructor and `Deconstruct`); include it in a custom `Equals`/`GetHashCode` (ADR-0011).
- DTOs are `internal sealed class` in `Dto/`, with `[JsonPropertyName]`. Mark a property `required` **only if the real API always returns it**: check with `/verify-api`.
- Mappers are `internal static class Mapper` per area. Validators derive from `ValueValidator<T>` in `Validation/` and are `internal sealed`.
- Public API needs `///` XML docs: a missing one is warning CS1591 in `src/`, and CI treats every warning as an error (`TreatWarningsAsErrors` when `ContinuousIntegrationBuild`; NuGet vulnerability warnings excepted). Mention API limits (date ranges, top count, publication schedule).
- Library code stays trimming- and Native AOT-safe: the .NET targets set `IsAotCompatible`, so reflection-based APIs show up as IL2xxx/IL3xxx warnings.
- Large clients are split into `partial` files by area (`NbpCurrencyExchangeRateClient.BuySell.cs`).
- Messages and logs are in English. Format dates invariantly (`yyyy-MM-dd`).

## Testing conventions

- One `partial` test class per production class, with one file per method: `NbpGoldPriceClientTest.GetLatestAsync.cs`. Shared helpers go in the main file (`NbpGoldPriceClientTest.cs`).
- Test names: `Method_Condition_ExpectedResult`. Use `// Arrange / // Act / // Assert` sections.
- Use deterministic data: `new Faker().WithConstantSeed()`, the DTO fakers in `tests/OpenUrzednik.Nbp.Tests/Fakes`, and `FakeTimeProvider`.
- Fake HTTP in unit tests with `StubHttpMessageHandler`; mock interfaces with NSubstitute. Pass `TestContext.Current.CancellationToken` to async calls.
- WireMock tests must use **response bodies captured from the real API**, not hand-written JSON. Each error path in [ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)'s Confirmation needs a WireMock test for each client: 400 with the server's message, 401 and 403, 404, 429 with `Retry-After`, 5xx, a 200 with an empty array (`NotFoundError`, for endpoints that return an array), malformed JSON, a timeout and a connection failure.
- Tests against the real API use `[ManualFact]`/`[ManualTheory]` (explicit tests) and must never run in default CI.
- Tests run on Microsoft.Testing.Platform (`global.json`), so filters are xUnit's (`--filter-class`, `--filter-method`, `--filter-trait`), not VSTest's `--filter`. Filter one project: on the whole solution, projects with no matching test make the run exit with code 8 (add `--ignore-exit-code 8`).

## Workflow

- Branch from `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`). PRs target `develop` and are merged with a merge commit (squash is disabled). Releases go from a `release/X.Y` branch, made from `develop`, into `main` ([ADR-0008](docs/adr/0008-branching-versioning-and-release.md)).
- A fix for a released version follows the hotfix procedure in [`docs/RELEASE-PROCESS.md`](docs/RELEASE-PROCESS.md): `hotfix/…` from the `vX.Y.Z` tag, the next patch in `version.json`, a PR into `main`, then the back-merge into `develop` through a `merge/vX.Y.Z` branch.
- Commit or push only when asked. One issue per PR (see "Issues and the project").
- If a change contradicts an accepted ADR, stop and use `/adr` instead of working around it. Until the first stable release, accepted ADRs are changed in place with a dated note (ADR-0001); from 1.0 on, a new superseding ADR.
- Before opening a PR, run the `reviewer` agent on the diff.
- The NuGet workflows build and pack `OpenUrzednik.Packages.slnf` (the `src/` projects only). **A new `src/` project must be added to it**, or it isn't published. Every package gets the version of the root `version.json` (lockstep, ADR-0008); don't add a `version.json` to a project. A new package input outside `src/` goes into both `pathFilters` in `version.json` and the `paths` filter of `nuget-pre-release.yml` (a directory is `dir` in the first and `dir/**` in the second).
- **CI job names are required status checks in the GitHub ruleset.** If you rename a job in `.github/workflows/build.yml`, `format.yml` or `dependency-review.yml`, say so: the ruleset must be updated (see `docs/GITHUB-SETUP.md`).
- Language ([ADR-0009](docs/adr/0009-documentation-language.md)): everything is in English, user-facing docs included (`README.md` in the root and per package, `CONTRIBUTING.md`, `SECURITY.md`, the samples README). English is the single source of truth: if a Polish text differs, the English one wins. `README.pl.md` next to the root README and each published package's README has only the informational parts: what the package is and the "unofficial" notice, installation, the quick-start example (the same code) and links to the English sections. Contract and reference content (API limits, data availability, errors, telemetry, configuration rules, dependencies, versioning, support) is English only. `SECURITY.pl.md` says how to report a vulnerability. Skeleton READMEs are English only; issue forms are bilingual inline. When a PR changes an informational part of a README, it updates `README.pl.md` too, and renaming or removing a section of an English README updates the links to it in `README.pl.md`.
- Prose style: when you write or edit user-facing prose (READMEs, `CONTRIBUTING`, `SECURITY`), run `/stop-slop` on the English files and `/miodkuj` on the Polish ones (`*.pl.md`). Edit only the prose: keep code blocks, commands, package and API names, badges, links and numbers exactly as they are. Meaning comes before style: keep API limits and qualifiers ("only", "never", "at most") even where a style rule says to cut them (stop-slop removes absolutes and adverbs).

## Issues and the project

Work is tracked in this repository's issues and in the public project [OpenUrzednik.NET](https://github.com/users/milten89/projects/2); its fields, views and workflows are described in [`docs/GITHUB-SETUP.md`](docs/GITHUB-SETUP.md#project). The issue body is the specification of the work.

- Take open issues with Status `Ready` and without the `maintainer-only` label, by Priority, then Seq. Only the maintainer moves an issue to `Ready`.
- Before you start, check that no open issue blocks it: `gh issue view <n> --json blockedBy --jq '[.blockedBy.nodes[] | select(.state == "OPEN") | .number]'` must print `[]`.
- When you start, set the issue's Status to `In progress`. That is the only field you change: never set `Done`, Priority, Phase, Seq, Size, Due, or the issue's milestone and labels unless asked.
- One issue per PR. The PR body starts with `Closes #<n>`. When you open the PR, give it at least one release label from `.github/release.yml` (`gh pr create --label <label>`), normally the one forecast on the issue.
- Never start, change or close a `maintainer-only` issue.
- You may open follow-up issues for problems outside the current issue. The project adds them with Status `Triage`, and the maintainer sorts them.
- Release and hotfix PRs target `main`, where `Closes` doesn't close the issue. The maintainer moves those issues to `Done`.

Project fields need the `project` scope (`gh auth refresh -s project`):

```bash
# Ready issues for agents, by Priority, then Seq (.id is the project item id)
gh project item-list 2 --owner milten89 --format json --limit 200 --jq '[.items[] | select(.status == "Ready" and .content.type == "Issue" and ((.labels // []) | index("maintainer-only") | not))] | sort_by(.priority // "P9", .seq) | .[] | "#\(.content.number) \(.priority) \(.id) \(.title)"'
# Set Status to In progress
gh project item-edit --project-id PVT_kwHOAJh3Rs4BnebI --id <item id> --field-id PVTSSF_lAHOAJh3Rs4BnebIzhmC35A --single-select-option-id e0ec58a2
```

## Skills and agents

- `/new-endpoint`: checklist for adding or changing a client method (DTO, mapper, validation, client, tests, docs).
- `/verify-api`: compare DTOs and test payloads with live API responses, and capture fixtures.
- `/adr`: create a new MADR record and update the index.
- `/miodkuj`: remove AI-sounding, bureaucratic and translated phrasing from Polish prose (vendored from [bartekpucek/miodkuj@32004e3](https://github.com/bartekpucek/miodkuj/tree/32004e3), MIT).
- `/stop-slop`: remove predictable AI writing patterns from English prose (vendored from [hardikpandya/stop-slop@8da1f03](https://github.com/hardikpandya/stop-slop/tree/8da1f03), MIT).
- `reviewer` agent: reviews a diff against the ADRs and the conventions above.
