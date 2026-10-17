---
name: new-endpoint
description: Checklist for adding or changing a provider client method (e.g. a new NBP API call) - DTO, mapper, validation, client method, telemetry, unit and WireMock tests, XML docs. Use whenever a public client method is added or its behaviour changes.
argument-hint: "<provider> <short description of the endpoint>"
---

# Add or change a client endpoint

Work through every step. Do not skip tests. If a step conflicts with an ADR, stop and raise it.

## 1. Understand the real API

- Find the endpoint in the official docs (NBP: https://api.nbp.pl/). Record the path, parameters, limits (date range, max count, earliest date) and status codes.
- **Fetch real responses** for success, empty or no data (404), and invalid input (400) with `/verify-api`. Save them as fixtures. Note which fields are actually present.

## 2. DTO (`src/<Provider>/Dto/`)

- `internal sealed class`, `[JsonPropertyName("…")]`, `init` properties.
- Mark a property `required` only if it appears in **every** real response. Otherwise make it nullable.
- Register the root type in the provider's `JsonSerializerContext` (e.g. `NbpJsonContext`).

## 3. Public model

- `public sealed record` with XML docs on every parameter. Use `IReadOnlyList<T>` for collections and override `Equals`/`GetHashCode` when the record holds a list.
- Dates: `DateOnly` (on netstandard2.0 it is an alias for `DateTime`, see ADR-0005). Money and rates: `decimal`.
- Check for name clashes with types in other namespaces of the same package.

## 4. Mapper

- Add it to the area's `internal static class Mapper`. Mappers must not throw on API data (ADR-0002). Handle nulls deliberately.

## 5. Validation

- Reuse or add an `internal sealed` `ValueValidator<T>` in `Validation/` for every documented limit. Combine results with `.And(...)`.
- Messages are in English with invariant formatting (`yyyy-MM-dd`) and name the parameter.

## 6. Client method

- Add it to the interface first, with XML docs that state limits and the publication schedule. `CancellationToken cancellationToken = default` goes last, in both the interface and the class.
- Follow the existing pattern: start the span (`<provider>.<area>.<operation>`, no `get_` prefix: `latest`, `today`, `top_count`, `date`, `range`) → set tags → combine the validators → `await _pipeline.GetAsync(...)` (whole payload) or `GetFirstAsync(...)` (first item of an array) with `.ConfigureAwait(false)`. The pipeline (`Nbp/Common/NbpRequestPipeline.cs`) logs and records validation failures, builds the path only after validation, sends the request, maps the payload and records failures. Put the URL builder call inside the path lambda (`() => builder.ForDate(date)`), never before it: builders reject invalid input.
- No `catch (Exception)`. Failures are returned as results, only caller cancellation is thrown (ADR-0002).
- An empty array where one item is expected → `NotFoundError`.

## 7. Tests

- **Unit** (`tests/OpenUrzednik.<Provider>.Tests/<Area>/<Client>Test.<Method>.cs`, a `partial` class):
  - success mapping
  - each validation rule (boundary values)
  - HTTP failure passthrough
  - empty response
  - telemetry: span name, tags, recorded errors
  - Use `Faker().WithConstantSeed()`, the DTO fakers, `StubHttpMessageHandler`, `FakeTimeProvider`.
- **Mapper** tests in `MapperTest.<MapMethod>.cs`; a **validator** test per new validator.
- **WireMock** (`tests/OpenUrzednik.IntegrationTests/<Provider>/WireMock/`):
  - success with the captured real body
  - every error path in the canonical list in [ADR-0002](../../../docs/adr/0002-result-pattern-and-error-handling.md)'s Confirmation
  - Assert the request path and `Accept` header.
- **Real API** test with `[ManualFact]` (explicit: runs with `--explicit on` or when `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED` is set).

## 8. Finish

- `dotnet build OpenUrzednik.slnx`, `dotnet test OpenUrzednik.slnx -f net10.0`, `dotnet format OpenUrzednik.slnx --verify-no-changes`.
- Update the package README (`README.md`, English) if the public API changed. If an informational part changed (description, installation, quick start, the list of English sections), update `README.pl.md` too (ADR-0009).
- The PR body starts with `Closes #<issue>` for the issue this work comes from.
- Run the `reviewer` agent on the diff.
