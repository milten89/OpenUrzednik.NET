---
paths:
  - "tests/**"
  - "src/**/*.cs"
---

# Testing conventions

- One `partial` test class per production class, with one file per method: `NbpGoldPriceClientTest.GetLatestAsync.cs`. Shared helpers go in the main file (`NbpGoldPriceClientTest.cs`).
- Test names: `Method_Condition_ExpectedResult`. Use `// Arrange / // Act / // Assert` sections.
- Use deterministic data: `new Faker().WithConstantSeed()`, the DTO fakers in `tests/OpenUrzednik.Nbp.Tests/Fakes`, and `FakeTimeProvider`.
- Fake HTTP in unit tests with `StubHttpMessageHandler`; mock interfaces with NSubstitute. Pass `TestContext.Current.CancellationToken` to async calls.
- A method that takes a personal identifier (NIP, REGON, KRS number, PESEL, bank account number) has a test that proves it doesn't leak (ADR-0003): give the client a TestCommon `RecordingTelemetry` as logger and trace source, and assert `result.ShouldNotLeak(telemetry, identifier)` for success and every error that can echo it. Use a fake identifier with a valid check digit whose digit runs can't appear by chance in the output. `RecordingTelemetry` doesn't see what `IHttpClientFactory` logs through Microsoft's `ILogger` (ADR-0017): a DI package of a provider with display paths tests that in its own tests.
- WireMock tests must use **response bodies captured from the real API**, not hand-written JSON.
  - Each error path in [ADR-0002](../../docs/adr/0002-result-pattern-and-error-handling.md)'s Confirmation needs a WireMock test for each client: 400 with the server's message, 401 and 403, 404, 429 with `Retry-After`, 5xx, a 200 with an empty array (`NotFoundError`, for endpoints that return an array), malformed JSON, a timeout and a connection failure.
  - If responses can contain personal data, capture them from the provider's test environment or from legal-person records, replace any remaining personal fields with fake values of the same format, and never commit API keys, session ids or tokens.
  - Hand-written bodies are only for cases the real API can't produce (an empty answer `[]`, malformed JSON).
- `tests/OpenUrzednik.Architecture.Tests` checks rules over the public API of every package (sealed classes and their `DesignedForInheritance` list, no public `DateOnly`, `CancellationToken` last and on every public async method, synchronous argument checks, records with collections, namespaces, error metadata, every error type public), the log calls of `src/` (`LoggingTest`: constant templates, a literal `OpenUrzednikLogLevel`, `IsEnabled` on the same logger before `Debug`/`Trace`, in every `#if` branch) and the project lists (`ProjectListsTest`: every `src/` project in the solution filter, the AotSmoke app, a shard of `shards.json` and the architecture tests; every `src/`, `tests/` and `samples/` project in the solution; every shard in the `mutation.yml` matrix). A failure names each offender: fix the code, and change a rule's exemption list only when an ADR allows the exception. ArchUnitNET covers the type-level rules; add an ADR's dependency rule there too (`Types().That()…Should().NotDependOnAny(…)`).
- Mutation testing ([ADR-0020](../../docs/adr/0020-mutation-testing-of-every-target-branch.md)): no mutant may stay undetected in a shard's strict files (`tests/Mutation/shards.json`), and a ratchet shard's score never drops below its `break`.
  - New or changed code in strict files comes with tests that kill its mutants; run its shard before the PR (CI runs the shards a PR into `develop` affects, `run.py --changed-since`).
  - `// Stryker disable once <Mutator> : <reason>` only for an equivalent mutant (no observable change, e.g. a performance-only path) or one Stryker can't activate (verified by hand), never for missing tests; it covers every mutant of the listed kinds in the whole statement that follows (a block statement included), so put it on the narrowest statement and check the report that it hides nothing a test kills.
  - Shared source that only tests compile goes into a test-only host library, or Stryker can't mutate it.
  - Shared source (`src/Identifiers`, `src/Privacy`, `src/Dates`, `src/Http`) is mutated in its host's shard, by its own test project, and never in a provider's shard; a provider's shard and its DI package's shard mutate only their own code (`!**/Http/**`, plus `!**/Identifiers/**`, `!**/Privacy/**` and `!**/Dates/**` where the provider imports them) and run only their own tests.
  - Every `#if` branch is mutated where a host can compile and run it: shared source with netstandard2.0 branches also has a host without the `NET` symbol and a `-netstandard` shard, which mutates the whole source again, built that way.
  - Its tests run with the same property, so an `#if NET` expectation in a test file must hold for both branches.
  - Keep netstandard2.0 branches free of the shapes Stryker can't compile, and reach a branch the .NET runtime never takes with a direct test, not a disable comment.
  - Polyfills stay excluded.
  - How to run shards, read reports and free mutants Stryker discards next to compile errors: `/stryker`.
