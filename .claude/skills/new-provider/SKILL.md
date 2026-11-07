---
name: new-provider
description: Checklist for adding a new provider package (e.g. MF White List, KRS, GUS BIR) and its DI package - every place in the repository a provider must touch (projects, solution filter, telemetry, tests, AOT smoke app, icon, release filters and scripts, READMEs, issue forms). Use when a provider ADR is accepted and its packages are created, and when reviewing such a PR.
argument-hint: "<package id, e.g. OpenUrzednik.MF.WhiteList>"
---

# Add a provider package

A provider PR ticks off every item below. Some fail the build or CI when missed (the allow-list, the AOT smoke app); others fail only at release or not at all (the solution filter, the release filters, the issue forms). The rules live in the ADRs and `CLAUDE.md`: this list says **where** each one applies. Endpoints follow `/new-endpoint`; fixtures follow `/verify-api`.

Notation, with MF White List as the example:

| Placeholder | Meaning | Example |
|---|---|---|
| `<Package>` | package id = root namespace = `ActivitySource` name | `OpenUrzednik.MF.WhiteList` |
| `<Name>` | the package id without `OpenUrzednik.` and dots: DI method names | `MFWhiteList` |
| `<Prefix>` | the type prefix the provider ADR chose | `WhiteList` |
| `<Provider>` | the folder name in shared test projects | `MFWhiteList` |
| `<provider>` | the short name in telemetry: snake_case of the id without `OpenUrzednik.` | `mf_white_list` |
| `<icon>` | the icon file name | `icon-mf` |
| `<label>` | the GitHub label | `provider:mf` |

## 0. Before any code

- [ ] The provider ADR is **accepted** and answers the whole readiness checklist of [ADR-0016](../../../docs/adr/0016-next-providers-and-package-naming.md), including the legal basis for automated access, the legal meaning of the output and partial batch results.
- [ ] Names follow ADR-0016: the table above. DI methods carry the package name, not the type prefix: `AddOpenUrzednik<Name>()`, `AddKeyedOpenUrzednik<Name>()`, `AddOpenUrzednik<Name>RejectionHandler()`.

## 1. Projects

- [ ] `src/<Package>/<Package>.csproj`:
  - `Description` ("Unofficial …", "Not affiliated with …"), `PackageTags`, `OpenUrzednikPackageIcon` = `<icon>.png`, `<None Include="README.md" Pack="true" PackagePath="\" Visible="false" />`;
  - **`IsPackable=false` until the package has a usable client** (ADR-0016): every merge into `develop` publishes previews of all packable projects;
  - an **empty** `PackageValidationBaselineVersion` until its first release;
  - `InternalsVisibleTo` for its test projects, and `DynamicProxyGenAssembly2` with Castle's `Key` if tests mock internals (copy it from `OpenUrzednik.Nbp.csproj`);
  - netstandard2.0-only BCL packages in a `Condition="'$(TargetFramework)' == 'netstandard2.0'"` group, never `Version=` (ADR-0004); the allow-list in `src/Directory.Build.targets` fails the build on anything else;
  - dates: `<Using Include="System.DateTime" Alias="DateOnly" />` on netstandard2.0 (ADR-0005). The `DateOnly` polyfills and `ToPublicDate()` are NBP-internal today (`src/OpenUrzednik.Nbp/Polyfills/DateOnlyPolyfills.cs`, `Extensions/DateOnlyExtensions.cs`): the first provider with dates moves them to shared source (preferred: one copy) or decides otherwise in its PR.
- [ ] Shared source: `src/Http/Http.props` (REST/JSON, ADR-0019), `src/Identifiers/Identifiers.props` (NIP, REGON, KRS, account numbers; brings `src/Privacy`), or `src/Privacy/Privacy.props` alone (masking only). A SOAP provider follows its ADR for the transport.
- [ ] Code skeleton, as NBP has it: `<Prefix>Options` (with `ApiUrl`) and its validator, the connection that builds the executor and its `RestProviderProfile` (NBP: `Nbp/Common/NbpConnection.cs`), the `JsonSerializerContext`, and clients with the two public constructors of ADR-0007.
- [ ] `src/<Package>.DependencyInjection`: links `src/Http/Infrastructure/ResilienceRejection.cs`, sets `OpenUrzednikAllowedPackageReferences`, returns the `IHttpClientBuilder`, adds `AddOpenUrzednik<Name>RejectionHandler()` (ADR-0004). If request paths or queries carry identifiers: imports `src/Http/DependencyInjection/DependencyInjection.props`, calls `AddOpenUrzednikDisplayPathLogging()` on every named client, and has a test that the factory's logging carries no identifier ([ADR-0017](../../../docs/adr/0017-http-client-logging-for-identifier-paths.md)).
- [ ] Every new project in `OpenUrzednik.slnx`; the `src/` ones also in **`OpenUrzednik.Packages.slnf`**.

## 2. Telemetry (ADR-0003, ADR-0015)

- [ ] `public static class <Prefix>Telemetry` with `SourceName = "<Package>"`, as `NbpTelemetry`.
- [ ] `public static class <Prefix>LogEvents`: a REST provider mirrors the `RestLogEvents` ids (1xxx); the provider's own events from 2001 on; new ids only, never reused. A SOAP provider's ADR decides its HTTP-level ids.
- [ ] `<InternalsVisibleTo Include="<Package>" />` in Core's .NET-only group (`src/OpenUrzednik.Core/OpenUrzednik.Core.csproj`): every provider that uses the .NET fallback trace source, SOAP ones too.
- [ ] Span names `<provider>.<area>.<operation>`, the HTTP span `<provider>.http.get` (from `RestProviderProfile`), tags `<provider>.<parameter>`; identifiers only masked or as display paths.

## 3. Tests

- [ ] `tests/<Package>.Tests` (unit, a `<Prefix>LogEventsTest` like `NbpLogEventsTest`) and `tests/<Package>.DependencyInjection.Tests`.
- [ ] `tests/OpenUrzednik.IntegrationTests`: a `ProjectReference` to each package, `<Provider>/WireMock/`, `<Provider>/Fixtures/` (copied to the output like `Nbp\Fixtures`), and `[ManualFact]` real-API tests.
- [ ] Mutation testing ([ADR-0018](../../../docs/adr/0018-mutation-testing-before-release.md)): a shard per project under test in `tests/Mutation/shards.json` (strict files: validators, mappers, models, URL building, error mapping, anything handling identifiers) with its `stryker-<shard>.json` (it mutates only the provider's code: `!**/Http/**`, plus `!**/Identifiers/**` and `!**/Privacy/**` if the provider imports them, since their hosts' shards mutate the shared source, and it runs only the provider's tests), and a matrix entry in `.github/workflows/mutation.yml`; `uv run tests/Mutation/run.py <shard>` passes before the first release. Use `/stryker` to run the shard, read its report, and free mutants Stryker discards next to compile errors.
- [ ] The adapter tests, with a `ProjectReference` in each test csproj: the fallback source in `tests/OpenUrzednik.Diagnostics.Tests` (like `NbpClientTracingTest`), logging and log-trace correlation in `tests/OpenUrzednik.Extensions.Logging.Tests` (like `NbpClientLoggingTest`, `NbpClientLogTraceCorrelationTest`).

## 4. Native AOT (ADR-0014)

- [ ] The dependencies and transport are AOT-safe: source-generated `JsonTypeInfo<T>` for JSON; for SOAP, no reflection-based `XmlSerializer`.
- [ ] `tests/OpenUrzednik.AotSmoke/OpenUrzednik.AotSmoke.csproj`: a `ProjectReference` and a `TrimmerRootAssembly` per package, and an `EmbeddedResource` per fixture it uses.
- [ ] `tests/OpenUrzednik.AotSmoke/Program.cs`: an offline call per client through a stub handler with a captured fixture (result, span, log entry), and the DI registration, default and keyed, as the NBP checks do.

## 5. Packaging and release (ADR-0008)

- [ ] Icon: an `[[icons.icon]]` entry in `assets/branding/branding.toml`, then `uv run assets/branding/generate.py` writes `<icon>.svg` and `<icon>.png`; a row in the "Package → icon" table of `assets/branding/BRANDING.md`.
- [ ] The icon PNG in `version.json` `pathFilters` and in the `paths` filter of `.github/workflows/nuget-pre-release.yml`, and in the icon list of `docs/RELEASE-PROCESS.md` (its `pathFilters` description).
- [ ] The package ids in `PACKAGES` of `.claude/skills/prepare-release/scripts/check_nuget.py`, and the package count in `.claude/skills/prepare-release/SKILL.md` ("the five packages").
- [ ] Each netstandard2.0 dependency the provider adds: a patch-only `ignore` entry and a pattern in the `published-dependencies` group of `.github/dependabot.yml`, unless an existing one covers it.
- [ ] `dotnet pack OpenUrzednik.Packages.slnf -c Release` passes package validation once the package is packable.

## 6. Docs and repository (ADR-0009)

- [ ] `src/<Package>/README.md` (what it is, the "unofficial" notice, installation, quick start, API limits, errors, telemetry reference, configuration) and `README.pl.md` (informational parts only, linking to the English sections with absolute `https://github.com/milten89/OpenUrzednik.NET/blob/main/…` URLs); the same for the DI package. `/stop-slop` on the English, `/miodkuj` on the Polish.
- [ ] Root `README.md` and `README.pl.md`: a row per package in the package table, and the intro and "next providers" paragraphs. `src/OpenUrzednik.Core/README.md` where it names the providers ("today `OpenUrzednik.Nbp`").
- [ ] `CLAUDE.md`: a row per package in the Project table.
- [ ] The package ids in the package dropdowns of `.github/ISSUE_TEMPLATE/bug_report.yml`, `feature_request.yml` and `0-question.yml`.
- [ ] The "missing or unexpected data" hints name the provider's source: `SUPPORT.md` (first bullet) and the intro of `.github/ISSUE_TEMPLATE/0-question.yml` (bilingual).
- [ ] Propose `<label>` with a row in the Labels table of `docs/GITHUB-SETUP.md`; the maintainer creates the label.

## 7. Finish

- [ ] `dotnet build OpenUrzednik.slnx`, the tests (`-f net10.0`, `-f net472`), `dotnet format OpenUrzednik.slnx --verify-no-changes`, the pack above, and the AOT smoke publish (`dotnet publish tests/OpenUrzednik.AotSmoke -f net10.0`) where the C++ build tools are installed.
- [ ] The `reviewer` agent on the diff; the PR starts with `Closes #<issue>`.
