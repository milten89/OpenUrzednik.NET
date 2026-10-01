# Backlog

Known bugs and design work, ordered by priority. Found in the repository review of 2026-10-01 and updated after the P0/P1 fixes (#12–#23). Per [ADR-0010](adr/0010-provider-readiness-gate.md), everything in P0–P2 must be done before any new provider is started.

Work through items one PR at a time. When an item is done, delete it from this file in the same PR. If an item turns into a GitHub issue, replace it here with a link to the issue.

## P0: Bugs

None open.

## P1: NBP correctness

27. **Gold price publication hour.** The XML docs say only "business days" because the hour couldn't be confirmed from NBP's own text (one source says 8:00–8:30). Confirm it and add it to `INbpGoldPriceClient`.

## P2: Framework (implements accepted ADRs)

12. **`OpenUrzednik.Http` package** ([ADR-0006](adr/0006-shared-http-layer.md)): move the request executor out of NBP and migrate NBP to it. `GetNbpAsync` (`Nbp/Extensions/HttpClientExtensions.cs`) already implements the ADR-0002 behaviour: disposal, status mapping, timeouts, network errors and a bounded read of 400 error bodies. What's missing is the provider-neutral package and per-provider overrides (e.g. how a provider's error body is turned into a message).
13. **Remove duplication in NBP clients.** About 30 methods repeat span → validate → log → GET → map → record. Mapping is shared since #18 (`NbpPayload.Map`); the rest still needs one internal pipeline helper (may come together with item 12).
14. **Client construction** ([ADR-0007](adr/0007-client-api-and-extensibility.md)): the default constructor `new NbpGoldPriceClient(httpClient)`; `INbpUrlBuilderFactory` optional with a default; remove the static cache in `NbpUrlBuilderFactory`.
15. **DI package** `OpenUrzednik.Nbp.DependencyInjection` ([ADR-0004](adr/0004-dependency-policy.md)): `AddOpenUrzednikNbp()`, typed clients, standard resilience handler, options validation.
16. **Telemetry adapters** ([ADR-0003](adr/0003-telemetry-abstractions.md)): `OpenUrzednik.Extensions.Logging` and `OpenUrzednik.OpenTelemetry`.
17. **netstandard2.0 target** ([ADR-0005](adr/0005-target-frameworks.md)): `DateTime` instead of `DateOnly`, polyfills, `System.Text.Json` and `Microsoft.Bcl.TimeProvider` only for that TFM, plus a .NET Framework test job. Decide the support window first (ADR-0005 open question).
18. **Result API ergonomics:** `Map`/`Bind`/`Match`/`TryGetValue`, implicit conversions from value and error, and an `Error` property on `OpenUrzednikException`.
26. **WireMock error paths for every client.** The 400/401/403/404/429/5xx, timeout, connection-failure and malformed-JSON tests run only through the gold client. That's enough while all clients share `GetNbpAsync`, but item 12 should add them per provider client.

## P3: Repository and quality

19. **Skeleton packages are published empty.** Set `IsPackable=false` for Gus, Krs and Mf ([ADR-0010](adr/0010-provider-readiness-gate.md)), and remove or keep their empty test projects (they produce "no tests available" warnings).
20. **Redundant SourceLink package.** The NU1902 warning is gone since #12 (`Microsoft.SourceLink.GitHub` 10.0.401), but SourceLink ships with the .NET 8+ SDK, so the `PackageReference` in `Directory.Build.props` can still be removed.
21. **Quality gates:**
    * Remove the `CS1591` suppression and document the public API. The NBP client interfaces and classes are documented (#23); options, URL builders, `HttpClientExtensions`, models and Core still have gaps.
    * `TreatWarningsAsErrors` in CI.
    * `EnablePackageValidation` with a baseline after the first stable release.
    * `IsAotCompatible`/`IsTrimmable` on .NET targets.
22. **English versions of user-facing docs** ([ADR-0009](adr/0009-documentation-language.md)): `README.en.md` (root and per package), `CONTRIBUTING.en.md`, with links from the Polish versions. Edit the prose with `/miodkuj` (Polish) and `/stop-slop` (English).
23. **Usage docs:** a `samples/` folder and package READMEs with real usage once the client API is settled (item 14).
24. **Move tests to the Microsoft.Testing.Platform runner.** `xunit.v3` 4.x no longer runs through VSTest on the .NET 10 SDK, so Dependabot's #11 fails CI. Add a `global.json` that opts in to the new `dotnet test`, update the CI workflow (`--collect:"XPlat Code Coverage"` in `build.yml` doesn't work under MTP), `integrationTest.runsettings`, coverlet and the commands in CLAUDE.md (`--filter` syntax changes), then let #11 rebase. Keep the `build (<tfm>)` job names: they are required status checks (or update the ruleset, see `docs/GITHUB-SETUP.md`).
25. **`docs/GITHUB-SETUP.md` is out of date.** "Restrict updates" was removed from the `protected-branches` ruleset (it made every merge an admin override), and the `code_quality` rule didn't block any merge, apparently because GitHub Code Quality isn't available for the repository (the setup API returns 404). Update the file and decide whether to keep that rule.
