# Backlog

Known bugs and design work, ordered by priority. Found in the repository review of 2026-10-01 and updated after the P0/P1 fixes (#12–#23). Per [ADR-0010](adr/0010-provider-readiness-gate.md), everything in P0–P2 must be done before any new provider is started.

How to use this file:

- Work through items one PR at a time.
- When an item is done, tick its checkbox and add `Done in #<PR>.` in the same PR. Don't delete items: a deletion conflicts with any branch that edits a neighbouring item (resolving that conflict wrongly is how items 8, 9 and 11 came back), and the record of what was done is lost.
- Keep a blank line between items, so two PRs that tick neighbouring items don't conflict. Two PRs that both add a new item at the end of the same section still conflict, and may pick the same number: renumber when resolving.
- Item numbers stay stable. New items get the next free number, in the section they belong to.
- Done items are removed only when they go into the release notes of a stable release (item 28).
- If an item turns into a GitHub issue, replace its text with a link to the issue.

## P0: Bugs

- [x] **1. Table B (`GetCountry*`) fails against the real API.** `CountryExchangeRatesDto` marked `country` and `symbol` as `required`, but the live API no longer returns them, so every call ended in `SerializationError`. Done in #13.

- [x] **2. `HttpRequestMessage` and `HttpResponseMessage` are never disposed** (`Nbp/Extensions/HttpClientExtensions.cs`). With `ResponseHeadersRead`, a non-success response held its connection until GC. Done in #14.

- [x] **3. Status mapping** ([ADR-0002](adr/0002-result-pattern-and-error-handling.md)): 5xx → `ServiceUnavailableError`; 400 → bad-request error carrying the NBP message; 401/403 → `UnauthorizedError`; status code in error metadata. Done in #16.

- [x] **4. Failures that escape the result** ([ADR-0002](adr/0002-result-pattern-and-error-handling.md)): `HttpRequestException` and timeouts were thrown, mappers threw `ArgumentException` when `Rates` was null, and `GetNbpAsync` had `catch (Exception)` blocks. Done in #18.

- [x] **5. `default(OpenUrzednikResult<T>)` is a success holding `null`.** Done in #15.

- [x] **6. `SerializationError.ToException()` returns the raw `JsonException`.** Done in #17.

## P1: NBP correctness

- [x] **7. Validators don't match the API.** Top count capped at 255, date ranges limited to 367 days (rates, gold) and 93 days (tables), future dates rejected using the Europe/Warsaw date. Done in #21.

- [x] **8. `NbpGoldPriceClient` methods don't default `CancellationToken`**, unlike the interface. Done in #20.

- [x] **9. Validation messages depend on culture** (`{date:d}`). Also fixed culture-dependent dates in URLs. Done in #19.

- [x] **10. Document publication schedules in XML docs.** Done in #23.

- [x] **11. Type name collisions:** `Currency.ExchangeRate` vs `Table.ExchangeRate`, and the same for `BuySellExchangeRate`. Done in #22.

- [ ] **27. Gold price publication hour.** The XML docs say only "business days" because the hour couldn't be confirmed from NBP's own text (one source says 8:00–8:30). Confirm it and add it to `INbpGoldPriceClient`.

- [ ] **33. Table top count doesn't match the API.** `exchangerates/tables/{t}/last/{n}` accepts at most 67 results for tables A and C and 14 for table B (checked 2026-10-02: A `last/68` returns 400 "Maximum size of 67 data series has been exceeded", B `last/20` the same with 14), apparently the tables published in the 93-day window. `TopCountValidator` allows 255 for every endpoint, so the table client sends requests the API rejects; the caller gets a `BadRequestError` instead of a `ValidationError`. The rates endpoint (`rates/b/{code}/last/255`) does accept 255. Decide on per-table limits.

- [ ] **34. Which side Buy and Sell are.** The table C models map the API's `ask` to `Buy` and `bid` to `Sell` (the customer's side), but NBP calls `bid` "kurs kupna" (buy rate), so a reader of the NBP docs expects the opposite. `CurrencyBuySellRate` and `TableBuySellRate` only say "Currency buy rate". Document the mapping, or rename to `Bid`/`Ask`, before 1.0.

## P2: Framework (implements accepted ADRs)

- [x] **12. `OpenUrzednik.Http` package** ([ADR-0006](adr/0006-shared-http-layer.md)): move the request executor out of NBP and migrate NBP to it. Done in #30.
    * `NbpConnection.GetAsync` (`Nbp/Common/NbpConnection.cs`, since #28) already implements the ADR-0002 behaviour: disposal, status mapping, timeouts, network errors and a bounded read of 400 error bodies.
    * Missing: the provider-neutral package, and per-provider overrides (e.g. how a provider's error body becomes a message).
    * Unexpected exceptions no longer mark the `nbp.http.get` span as an error (#18 removed the catch-all). Fix it with try/finally, not `catch (Exception)`.
    * Typo in the `UnknownError` message: "NBP API return unknown status".

- [x] **13. Remove duplication in NBP clients.** 25 methods repeat span → validate → log → GET → map → record. Mapping is shared since #18 (`NbpPayload.Map`); the rest needs one internal pipeline helper (after items 12 and 18). Done in #31 (`Nbp/Common/NbpRequestPipeline.cs`).
    * No `await` in the three clients uses `ConfigureAwait(false)`. Fixed: every await uses it.
    * The empty-array `NotFoundError` has no status code, unlike a real 404. Kept: the response was `200 OK`, so there is no 404 to report; documented on the pipeline.
    * Span names are inconsistent: only date and range use a `get_` prefix (`get_date`, `get_range` vs `latest`, `today`, `top_count`), and the buy/sell variants drop it (`buy_sell_date`, `buy_sell_range`). Fixed by dropping the prefix: `date`, `range`.

- [x] **14. Client construction** ([ADR-0007](adr/0007-client-api-and-extensibility.md)): the default constructor `new NbpGoldPriceClient(httpClient)`; `NbpOptions` and `INbpUrlBuilderFactory` optional; remove the static cache in `NbpUrlBuilderFactory`. Done in #28.
    * Remove `ConfigureForNbpApi`: it mutates a caller-owned `HttpClient` and is a second way to configure the client. Options go to the constructor; its checks move to `NbpOptions` validation.
    * Base URL: `NbpOptions.ApiUrl`, then `HttpClient.BaseAddress`, then `NbpOptions.DefaultApiUrl`. Today a missing `BaseAddress` makes `HttpClient` throw `InvalidOperationException`, which escapes the result.
    * `NbpOptions.Timeout` becomes optional; when unset, the `HttpClient`'s own timeout applies (.NET default 100 s).
    * `NbpUrlBuilderFactoryTest` asserts the static cache (`*_ReturnsSameCachedInstance`, `GetTableBuilder_SameTableOnDifferentFactoryInstances_*`). The constructor tests still read private fields: moved to item 30.

- [x] **15. DI package** `OpenUrzednik.Nbp.DependencyInjection` ([ADR-0004](adr/0004-dependency-policy.md)): `AddOpenUrzednikNbp()`, typed clients, options validation. Done in #34.
    * Resilience follows the .NET standard: `AddOpenUrzednikNbp()` returns the `IHttpClientBuilder`, and the app opts in with `.AddStandardResilienceHandler()`. One handler, not stacked.
    * Polly's rejections (`TimeoutRejectedException`, circuit breaker, rate limiter) must be converted into errors, or they escape the result.
    * Register the clients with factory lambdas (`AddHttpClient<T>((http, sp) => new T(http, ...))`): a bare `AddHttpClient<T>()` fails, because `ActivatorUtilities` finds two public constructors that accept an `HttpClient` (ADR-0007 asks for both).
    * Version mismatch: `Microsoft.Extensions.DependencyInjection.Abstractions` is 10.0.0, while `Logging.Abstractions`, `Http` and `Options` are 10.0.10. Aligned to 10.0.10.
    * Which `Microsoft.Extensions.*` major to reference per target moved to item 35.

- [x] **16. Telemetry adapters** ([ADR-0003](adr/0003-telemetry-abstractions.md)): `OpenUrzednik.Extensions.Logging` (`ILogger`) and `OpenUrzednik.Diagnostics` (`ActivitySource`). The core packages keep the custom interfaces, so they need no dependencies on .NET Framework 4.8. Done in #33.
    * Rename tags per ADR-0003: `http.status_code` → `http.response.status_code`, `http.path` → `url.path`. Done; `http.request.method` added, and `url.path` is now the absolute path (`/api/cenyzlota`) instead of the relative one.
    * Decide the logger category per client. The abstraction has no `ActivityKind`. Decided: the category is the client's full type name (as `ILogger<T>`), and every span is `Internal`, because `HttpClient`'s own instrumentation creates the client span.

- [ ] **17. netstandard2.0 target** ([ADR-0005](adr/0005-target-frameworks.md)): `DateTime` instead of `DateOnly` on that target, polyfills, `System.Text.Json` and `Microsoft.Bcl.TimeProvider` only for it, plus a .NET Framework test job.
    * Blockers: ~45 `DateOnly` sites, ~30 `ThrowIf*` calls, `required`/`init`/records (polyfills), `HashCode`, `[GeneratedRegex]`, `HttpStatusCode.TooManyRequests`, `MediaTypeNames`, `ReadAsStreamAsync(ct)`, `Memory<char>` reads, `Enum.IsDefined<T>`, `[MaybeNullWhen]` and `string.Create(IFormatProvider, …)` in Core, and ranges/`EndsWith(char)` in `NbpUrlBuilder`. The adapters add `ThrowIfNegative`/`ThrowIfGreaterThan`/`ThrowIfNullOrWhiteSpace`, span `string.Concat`, `StringBuilder.Append(ReadOnlySpan<char>)` and span ranges in `LogValues`, and `OpenUrzednik.Diagnostics` needs a netstandard2.0-only `System.Diagnostics.DiagnosticSource` reference. On .NET Framework, `HttpClient.Timeout` has no inner `TimeoutException`, so `RestRequestExecutor.ElapsedLimit` (#34) reports such a timeout without a limit; consider measuring the elapsed time instead.

- [x] **18. Result API ergonomics:** `Map`/`Bind`/`Match`/`TryGetValue`, and an `Error` property on `OpenUrzednikException`. Done in #29.
    * `EnsureSuccess` throws `AggregateException` for several errors, against ADR-0002. Several errors come only from validation, so throw one `ValidationException` carrying all of them.
    * Exceptions lose their error and its metadata. `ToException()` returns `Exception` instead of `OpenUrzednikException`.
    * Forwarding a failure copies the error array twice.
    * Implicit conversions from a value or an error to `OpenUrzednikResult<T>` were considered and rejected (they don't apply to interface types and are ambiguous for `object`). Only `OpenUrzednikError` → non-generic `OpenUrzednikResult`.
    * The BCL name clash moved to item 32.

- [x] **26. WireMock error paths for every client.** The 400/401/403/404/429/5xx, timeout, connection-failure and malformed-JSON tests run only through the gold client. Add them for the currency and table clients after item 12. Done in #32 (`*WireMockTest.Errors.cs`, plus the empty-array case).
    * The currency and table WireMock success bodies are hand-written, and so are the gold ones (including `NbpGoldPriceClientWireMockTest.Construction.cs`). Capture fixtures with `/verify-api`. Done in #32: every success body is a fixture captured on 2026-10-02 (`Nbp/Fixtures`, table rates trimmed to 3).

- [ ] **35. `Microsoft.Extensions.*` versions per target.** The integration packages (`OpenUrzednik.Extensions.Logging`, `OpenUrzednik.Nbp.DependencyInjection`) reference the 10.0.x packages on every target, so an ASP.NET Core 8 app that adds them moves `DiagnosticSource`, `DependencyInjection.Abstractions`, `Logging.Abstractions`, `Http` and `Options` to 10.x. Microsoft's own packages (e.g. `Microsoft.Extensions.Resilience` 10.8) reference the lowest version per target: 8.0.x for net8.0, 9.0.x for net9.0. Decide whether to do the same (TFM-conditional `PackageVersion`s) or record in ADR-0004 that integration packages track the latest major.

## P3: Repository and quality

- [ ] **19. Skeleton packages are published empty.** Set `IsPackable=false` for Gus, Krs and Mf ([ADR-0010](adr/0010-provider-readiness-gate.md)), Their empty test projects were removed in #35: under Microsoft.Testing.Platform a test project with no tests fails the run (exit code 8). Add a test project together with a provider's first tests.

- [ ] **20. Redundant SourceLink package.** The NU1902 warning is gone since #12 (`Microsoft.SourceLink.GitHub` 10.0.401), but SourceLink ships with the .NET 8+ SDK, so the `PackageReference` in `Directory.Build.props` can still be removed.

- [ ] **21. Quality gates:**
    * Remove the `CS1591` suppression and document the public API. The NBP client interfaces and classes are documented (#23); options, URL builders, `HttpClientExtensions`, models and Core still have gaps.
    * `TreatWarningsAsErrors` in CI.
    * `EnablePackageValidation` with a baseline after the first stable release.
    * `IsAotCompatible`/`IsTrimmable` on .NET targets.

- [ ] **22. English versions of user-facing docs** ([ADR-0009](adr/0009-documentation-language.md)): `README.en.md` (root and per package), `CONTRIBUTING.en.md`, with links from the Polish versions. Edit the prose with `/miodkuj` (Polish) and `/stop-slop` (English).

- [ ] **23. Usage docs:** a `samples/` folder and package READMEs with real usage once the client API is settled (item 14).

- [x] **24. Move tests to the Microsoft.Testing.Platform runner.** `xunit.v3` 4.x no longer runs through VSTest on the .NET 10 SDK, so Dependabot's #11 fails CI.
    * Add a `global.json` that opts in to the new `dotnet test`.
    * Update the CI workflow (`--collect:"XPlat Code Coverage"` in `build.yml` doesn't work under MTP), `integrationTest.runsettings`, coverlet and the commands in CLAUDE.md (`--filter` syntax changes), then let #11 rebase.
    * Keep the `build (<tfm>)` job names: they are required status checks (or update the ruleset, see `docs/GITHUB-SETUP.md`).
    * Best done before item 17, which adds a .NET Framework test job.
    * Done in #35, together with the xUnit 4 and Microsoft.NET.Test.Sdk bumps from #11 (Microsoft's coverage extension needs MTP v2, which xUnit 4 uses). #11 stays open for its NSubstitute and WireMock.Net bumps, after a rebase. Coverage comes from `Microsoft.Testing.Extensions.CodeCoverage` (`--coverage`) instead of coverlet. `integrationTest.runsettings` is gone, because MTP doesn't read it: the real-API tests are explicit and run with `--explicit on`.

- [ ] **25. `docs/GITHUB-SETUP.md` is out of date.** "Restrict updates" was removed from the `protected-branches` ruleset (it made every merge an admin override), and the `code_quality` rule didn't block any merge, apparently because GitHub Code Quality isn't available for the repository (the setup API returns 404). Update the file and decide whether to keep that rule.

- [ ] **28. Release notes for 1.0.** There is no CHANGELOG, while #13, #15, #16, #17, #18, #21 and #22 changed the API or behaviour (`.github/release.yml` only generates notes from PR titles). Write the notes for the first stable release, and move the done items here into them.

- [ ] **29. Docs polish.**
    * ADR-0002 L49 still says "(backlog item)" for `default(OpenUrzednikResult<T>)`.
    * The summaries of `CurrencyExchangeRates`, `BuySellExchangeRates`, `ExchangeRateTable` and `BuySellExchangeRateTable` don't name the table (A/B or C).
    * The publication-schedule sentence is repeated on every client method. Optional: keep it on the interface and the Today/Latest methods only.

- [ ] **30. Test conventions.**
    * Core tests aren't `partial` or file-per-method (CLAUDE.md), and `NetworkErrorsTest` covers two production classes.
    * `CultureScope` (TestCommon) needs ICU: it fails under `InvariantGlobalization`. Note it in the class docs.
    * The client constructor tests (`*Test.ctor.cs`) read private fields with `GetPrivateField`. Test through behaviour instead (e.g. which URL builder and clock a request uses).

- [ ] **31. Prose skills follow-ups** (#24).
    * Say that meaning, API limits and qualifiers ("only", "never") take precedence over style rules: stop-slop removes absolutes.
    * Make `AGENTS.md` protect the same items as `CLAUDE.md` (badges, numbers, which README is Polish).
    * Record the upstream versions: miodkuj@32004e3, stop-slop@8da1f03.

- [ ] **32. Exception names clash with the BCL.** `SerializationException` (`System.Runtime.Serialization`) and `ValidationException` (`System.ComponentModel.DataAnnotations`) need an alias when both namespaces are imported. Decide before 1.0 whether to rename them (e.g. `OpenUrzednikValidationException`).
