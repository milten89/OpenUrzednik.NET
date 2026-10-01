# Backlog

Known bugs and design work, ordered by priority. Found in the repository review of 2026-10-01. Per [ADR-0010](adr/0010-provider-readiness-gate.md), everything in P0–P2 must be done before any new provider is started.

Work through items one PR at a time. When an item is done, delete it from this file in the same PR. If an item turns into a GitHub issue, replace it here with a link to the issue.

## P0: Bugs

1. **Table B (`GetCountry*`) fails against the real API.** `CountryExchangeRatesDto` marks `country` and `symbol` as `required`, but the live API no longer returns them (`{"table":"B","currency":"afgani (Afganistan)","code":"AFN","rates":[…]}`), so every call ends in `SerializationError`. The WireMock tests use made-up payloads that include `country`, which is why they pass. Fix: make the fields optional, rename the API (table B is "less common currencies", not "country"), and replace the test payloads with captured real responses (`/verify-api`).
4. **Failures that escape the result** ([ADR-0002](adr/0002-result-pattern-and-error-handling.md)): `HttpRequestException` and timeouts are thrown; mappers throw `ArgumentException` when `Rates` is null; `catch (Exception)` blocks in `GetNbpAsync`.
5. **`default(OpenUrzednikResult<T>)` is a success holding `null`.** Store an explicit state so `default` is not a success.
6. **`SerializationError.ToException()` returns the raw `JsonException`.** Wrap it in an `OpenUrzednikException`-derived type and keep the original as `InnerException`.

## P1: NBP correctness

7. **Validators don't match the API.** `TopCountValidator` must cap at 255 (`last/256` returns 400). Re-check the date-range limit: the code enforces 93 days, the live API accepted 367 days on 2026-10-01, and the message says "less than" while the check allows equality. Reject future dates using the injected `TimeProvider` (Europe/Warsaw date).
8. **`NbpGoldPriceClient` methods don't default `CancellationToken`**, unlike the interface.
9. **Validation messages depend on culture** (`{date:d}`). Use invariant `yyyy-MM-dd`.
10. **Document publication schedules in XML docs.** Table B is published on Wednesdays (`GetTodayAsync(B)` usually returns 404), and rates appear around midday Warsaw time.
11. **Type name collisions:** `Currency.ExchangeRate` vs `Table.ExchangeRate`, and the same for `BuySellExchangeRate`.

## P2: Framework (implements accepted ADRs)

12. **`OpenUrzednik.Http` package** ([ADR-0006](adr/0006-shared-http-layer.md)): move the request executor out of NBP, with timeout detection, disposal, status mapping and provider overrides. Migrate NBP to it.
13. **Remove duplication in NBP clients.** About 30 methods repeat span → validate → log → GET → map → record. Extract one internal pipeline helper (may come together with item 12).
14. **Client construction** ([ADR-0007](adr/0007-client-api-and-extensibility.md)): the default constructor `new NbpGoldPriceClient(httpClient)`; `INbpUrlBuilderFactory` optional with a default; remove the static cache in `NbpUrlBuilderFactory`.
15. **DI package** `OpenUrzednik.Nbp.DependencyInjection` ([ADR-0004](adr/0004-dependency-policy.md)): `AddOpenUrzednikNbp()`, typed clients, standard resilience handler, options validation.
16. **Telemetry adapters** ([ADR-0003](adr/0003-telemetry-abstractions.md)): `OpenUrzednik.Extensions.Logging` and `OpenUrzednik.OpenTelemetry`.
17. **netstandard2.0 target** ([ADR-0005](adr/0005-target-frameworks.md)): `DateTime` instead of `DateOnly`, polyfills, `System.Text.Json` and `Microsoft.Bcl.TimeProvider` only for that TFM, plus a .NET Framework test job. Decide the support window first (ADR-0005 open question).
18. **Result API ergonomics:** `Map`/`Bind`/`Match`/`TryGetValue`, implicit conversions from value and error, and an `Error` property on `OpenUrzednikException`.

## P3: Repository and quality

19. **Skeleton packages are published empty.** Set `IsPackable=false` for Gus, Krs and Mf ([ADR-0010](adr/0010-provider-readiness-gate.md)), and remove or keep their empty test projects (they produce "no tests available" warnings).
20. **NU1902 vulnerability:** `Microsoft.SourceLink.GitHub 10.0.301` pulls in a vulnerable `Microsoft.Build.Tasks.Git`. SourceLink ships with the .NET 8+ SDK, so remove the package reference.
21. **Quality gates:**
    * Remove the `CS1591` suppression and document the public API.
    * `TreatWarningsAsErrors` in CI.
    * `EnablePackageValidation` with a baseline after the first stable release.
    * `IsAotCompatible`/`IsTrimmable` on .NET targets.
22. **English versions of user-facing docs** ([ADR-0009](adr/0009-documentation-language.md)): `README.en.md` (root and per package), `CONTRIBUTING.en.md`, with links from the Polish versions.
23. **Usage docs:** a `samples/` folder and package READMEs with real usage once the client API is settled (item 14).
