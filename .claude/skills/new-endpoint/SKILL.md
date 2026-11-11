---
name: new-endpoint
description: Checklist for adding or changing a provider client method (an NBP API call, or one of a newer provider) - DTO, mapper, validation, client method, telemetry, unit and WireMock tests, XML docs. Use whenever a public client method is added or its behaviour changes.
argument-hint: "<provider> <short description of the endpoint>"
---

# Add or change a client endpoint

Work through every step. Do not skip tests. If a step conflicts with an ADR, stop and raise it. A new provider's packages come first, with `/new-provider`. Placeholders as in `/new-provider`: `<Package>` is the package id (`OpenUrzednik.Nbp`), `<Provider>` its folder in shared test projects (`Nbp`). NBP names below are the example; a provider uses its own type prefix (`<Prefix>`), and a SOAP provider (GUS) follows its provider ADR where a step concerns the REST transport.

## 1. Understand the real API

- Find the endpoint in the official docs (NBP: https://api.nbp.pl/; others: the provider ADR links them). Record the path, parameters, limits (date range, max count, earliest date, quota) and status codes, and whether the path or query carries a personal identifier (ADR-0003).
- **Fetch real responses** for success, empty or no data (404), and invalid input (400) with `/verify-api`. Save them as fixtures. Note which fields are actually present.

## 2. DTO (`src/<Package>/Dto/`)

- `internal sealed class`, `[JsonPropertyName("…")]`, `init` properties.
- Mark a property `required` only if it appears in **every** real response. Otherwise make it nullable.
- Register the root type in the provider's `JsonSerializerContext` (NBP: `NbpJsonContext`).
- SOAP (GUS): XML contracts and their handling follow the provider ADR, AOT-safe (ADR-0014): no reflection-based `XmlSerializer`.

## 3. Public model

- `public sealed record` with XML docs on every parameter. Use `IReadOnlyList<T>` for collections and override `Equals`/`GetHashCode` when the record holds a list.
- Adding data to an existing model: a non-`required` `init` property, not a new positional parameter, and include it in a custom `Equals`/`GetHashCode` (ADR-0011).
- Dates: public parameters and model properties are `DateTime` on every target (ADR-0005): convert arguments with `DateOnly.FromDateTime` at the start of the method, return `ToPublicDate()` from the mapper, and add the `DateOnly` overload to the provider's `<Prefix>DateOnlyExtensions` (.NET only; NBP: `NbpDateOnlyExtensions`). DTOs, validators and URL builders use `DateOnly` (an alias for `DateTime` on netstandard2.0). Money and rates: `decimal`.
- Check for name clashes with types in other namespaces of the same package.

## 4. Mapper

- Add it to the area's `internal static class Mapper`. Mappers must not throw on API data (ADR-0002). Handle nulls deliberately.

## 5. Validation

- Reuse or add an `internal sealed` `ValueValidator<T>` in `Validation/` for every documented limit. Combine results with `.And(...)`.
- Messages are in English with invariant formatting (`yyyy-MM-dd`) and name the parameter.
- A NIP, REGON, KRS number or bank account number uses the shared validator from `src/Identifiers` (the provider imports `Identifiers.props`): throw `ArgumentNullException` for `null` first, validate, and send the validator's `Normalized` digits, never the input. Its errors already mask the value; any other message or log that mentions an identifier masks it with `SensitiveValue.Mask` (ADR-0003).

## 6. Client method

- SemVer (ADR-0011): a new method on a client interface is a minor change. In a package that has been released, package validation reports it as CP0006: suppress exactly that diagnostic for that method. A package not released yet (an empty `PackageValidationBaselineVersion`) has no baseline, so it gets no CP0006 and needs no suppression. Changing or removing a method, or changing documented behaviour, needs a major version (`breaking-change` label, `version.json` raised). A patch adds no new method.
- Add it to the interface first, with XML docs that state limits and the publication schedule. `CancellationToken cancellationToken = default` goes last, in both the interface and the class.
- Follow the existing pattern: start the span (`<provider>.<area>.<operation>`, no `get_` prefix: `latest`, `today`, `top_count`, `date`, `range`) → set tags → combine the validators → the provider's request pipeline with `.ConfigureAwait(false)` (NBP: `await _pipeline.GetAsync(...)` for the whole payload or `GetFirstAsync(...)` for the first item of an array). The pipeline (NBP: `Nbp/Common/NbpRequestPipeline.cs`) logs and records validation failures, builds the path only after validation, sends the request, maps the payload and records failures. Put the URL builder call inside the path lambda (`() => builder.ForDate(date)`), never before it: builders reject invalid input.
- A path or query that carries a personal identifier goes through the executor's display-path overload, `GetAsync(relativePath, displayPath, typeInfo, …)`, with a route template or masked display path (ADR-0019): it replaces the URI in `url.path`, logs, error messages and `MapErrorAsync`'s `RequestPath`, and the default 400 mapping drops the server's message. The DI package logs the same display path ([ADR-0017](../../../docs/adr/0017-http-client-logging-for-identifier-paths.md)).
- No `catch (Exception)`. Failures are returned as results; only caller cancellation and argument errors are thrown (ADR-0002). A `null` argument (`ArgumentNullException.ThrowIfNull`) or an undefined enum value (`ArgumentGuard.ThrowIfUndefined`) throws synchronously: the public method is not `async`, checks them and returns `<Method>CoreAsync(...)`. Format and range checks stay validators that return a `ValidationError`.
- An empty array where one item is expected → `NotFoundError`.

## 7. Tests

- **Unit** (`tests/<Package>.Tests/<Area>/<Client>Test.<Method>.cs`, a `partial` class):
  - success mapping
  - each validation rule (boundary values)
  - HTTP failure passthrough
  - empty response
  - telemetry: span name, tags, recorded errors (`error.type`); a new log call gets a new event id (`RestLogEvents` for the HTTP layer, the provider's events and `<Prefix>LogEvents` for the rest; NBP: `NbpEvents` + `NbpLogEvents`), never a reused one
  - identifiers: the value appears nowhere unmasked (messages, metadata, log arguments, span tags), with a fake identifier that passes its check digits
  - Use `Faker().WithConstantSeed()`, the DTO fakers, `StubHttpMessageHandler`, `FakeTimeProvider`.
- **Mapper** tests in `MapperTest.<MapMethod>.cs`; a **validator** test per new validator.
- **WireMock** (`tests/OpenUrzednik.IntegrationTests/<Provider>/WireMock/`):
  - success with the captured real body; if responses can contain personal data, capture from the provider's test environment or from legal-person records, replace remaining personal fields with fake values of the same format, and never commit API keys, session ids or tokens. Hand-written bodies are only for cases the real API can't produce (an empty answer `[]`, malformed JSON)
  - every error path in the canonical list in [ADR-0002](../../../docs/adr/0002-result-pattern-and-error-handling.md)'s Confirmation
  - Assert the request path and `Accept` header.
- **Real API** test with `[ManualFact]` (explicit: runs with `--explicit on` or when `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` is set). Against the provider's test environment when it has one, and never spending a scarce daily quota.

## 8. Finish

- `dotnet build OpenUrzednik.slnx -p:TreatWarningsAsErrors=true`, `dotnet test OpenUrzednik.slnx -f net10.0`, `dotnet format OpenUrzednik.slnx --verify-no-changes`.
- A new or changed public method changes a package's API, so also run `dotnet pack OpenUrzednik.Packages.slnf -c Release` (package validation; see `CLAUDE.md`, Commands).
- `dotnet test OpenUrzednik.slnx -f net472` on Windows (the netstandard2.0 build on .NET Framework; CI runs it as `test (net472)`).
- The provider's mutation shard: `uv run tests/Mutation/run.py <shard>` (`/stryker`). The provider shards have `break` 100 (`tests/Mutation/shards.json`), so no mutant may survive.
- The Native AOT smoke app, when the method adds a DTO, a `JsonTypeInfo` or other serialization code: add an offline call with a captured fixture to `tests/OpenUrzednik.AotSmoke/Program.cs` for a new response type, then `dotnet publish tests/OpenUrzednik.AotSmoke -f net10.0` and run the binary (needs clang on Linux or the C++ build tools on Windows; CI does both as `aot (net10.0)`). The publish checks all library code, since every assembly is rooted; the run exercises only the calls in `Program.cs`.
- The provider's notes in `/verify-api` (endpoint, error cases, quota, test environment), in the same PR.
- Update the package README (`README.md`, English) if the public API changed. Add a new span, tag or log event to its telemetry reference. If an informational part changed (description, installation, quick start, the list of English sections), update `README.pl.md` too (ADR-0009).
- The PR body starts with `Closes #<issue>` for the issue this work comes from.
- Run the `reviewer` agent on the diff.
