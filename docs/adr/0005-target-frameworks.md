---
status: accepted
date: 2026-10-29
decision-makers: milten89
---

# 0005. Target frameworks and one public API

## Context and Problem Statement

Many companies integrating with Polish public APIs still run .NET Framework applications. Modern .NET offers better types (`DateOnly`, `TimeProvider`, generated regexes) that we want to use. Which target frameworks do we support and how do we deal with APIs missing on older targets?

## Decision Drivers

* Reach .NET Framework 4.7.2+ consumers.
* Use modern types inside the library on modern .NET without slowing it down.
* One public API for every build, so a library built for netstandard2.0 on top of ours works in .NET Framework and .NET apps alike.
* No extra dependencies on .NET targets ([ADR-0004](0004-dependency-policy.md)).

## Considered Options

* netstandard2.0 + the supported .NET LTS releases, `#if` for differences
* Modern .NET only
* Uniform API (`DateTime` everywhere or a custom date struct)

## Decision Outcome

Chosen option: "netstandard2.0 + the supported .NET LTS releases, `#if` for differences", with the public dates taken from "Uniform API (`DateTime` everywhere…)": modern types stay inside the library and in .NET-only additions.

* Target frameworks: `netstandard2.0;net8.0;net10.0`: netstandard2.0 plus the .NET LTS releases in support. A newer target (an STS release) is added only when it brings APIs we use. Apps on a .NET version without its own target get the nearest lower one (a .NET 9 app gets `net8.0`).
* .NET Framework: **4.7.2 or later**, through netstandard2.0. On 4.6.1–4.7.1, netstandard2.0 libraries need extra facade assemblies (about 100 on 4.6.1–4.7.0) or binding redirects; Microsoft recommends 4.7.2 or later.
* **One public API for every build:** every build has the same public signatures for what it shares; the .NET builds may add members, never change them. Public dates are `DateTime` on every target, in parameters and model properties: the time of day and `Kind` of an argument are ignored, and returned dates are `00:00` with `Kind = Unspecified`. On .NET 8 and later, `DateOnly` overloads come as extension methods (`NbpDateOnlyExtensions`), not as interface members. Internally the code uses `DateOnly` (an alias for `DateTime` on netstandard2.0) and converts only at the public boundary.
* Differences are handled with preprocessor directives (`#if NET` / `#else`) as locally as possible – prefer small internal helpers/polyfills (e.g. `ThrowHelper` for `ArgumentNullException.ThrowIfNull`) over scattering `#if` through business code.
* netstandard2.0-only dependencies are limited to official Microsoft BCL packages (`System.Text.Json`, `Microsoft.Bcl.TimeProvider`, `System.Net.Http.Json` if needed).
* Every TFM is built and tested in CI. netstandard2.0 is tested by compiling the tests for `net472`, which proves no newer .NET Framework API is used, and running them on the Windows runner's .NET Framework 4.8.x. A machine has exactly one 4.x runtime and 4.8 is a compatibility update of 4.7.2, so a separate 4.7.2 run is neither needed nor possible on GitHub runners.
* Support window:
  * A **.NET target that isn't the lowest one** may be removed in a minor version, once Microsoft has ended its support: apps on that version get the next lower .NET target, with the same API.
  * The **lowest .NET target** (today `net8.0`) and **netstandard2.0** are removed only in a major version, because apps would then get a different build or none ([ADR-0008](0008-branching-versioning-and-release.md) allows breaking changes only in a major). netstandard2.0 is a specification, not a runtime, so it has no end of support; through it the library runs on .NET Framework 4.8, which Microsoft supports as part of Windows.

### Consequences

* Good, because the library is usable from legacy enterprise code.
* Good, because one netstandard2.0 library built on these packages works in .NET Framework and .NET apps alike, and package validation needs no suppressions.
* Bad, because .NET callers see `DateTime` in models and interfaces, where `DateOnly` would say more; the `DateOnly` overloads cover only the method parameters.
* Bad, because netstandard2.0 testing requires a Windows CI job.

### Confirmation

CI matrix includes all TFMs; package validation (`EnablePackageValidation`) checks that the builds of a package are compatible with each other, without suppressions. The `reviewer` agent flags a public `DateOnly` and any interface member that exists only on .NET.

## More Information

**2026-10-02 implementation note** (backlog item 17):

* Dates: the code says `DateOnly` on every target. On netstandard2.0 a global using makes it an alias for `DateTime`, so the public API there takes and returns `DateTime` (public dates are `DateTime` on every target since 2026-10-29, see below). `Nbp/Polyfills/DateOnlyPolyfills.cs` adds the `DateOnly` members the code needs as C# 14 extension members. Validators compare `DayNumber`, so a time of day is ignored rather than rejected.
* Polyfills are internal, compiled only for netstandard2.0, one copy per assembly: compiler attributes (init, required members, `CallerArgumentExpression`), nullable attributes the `ArgumentNullException.ThrowIfNull` family, `Enum.IsDefined<T>`, `string.Create(IFormatProvider, …)`, `string.EndsWith(char)` and, in `OpenUrzednik.Extensions.Logging`, the span `StringBuilder.Append`/`string.Concat` overloads (C# 14 extension members, so call sites don't change and the .NET code stays as fast as before). The JSON body read is bounded with `WaitAsync` on netstandard2.0 only, in a small `#if` helper, because a polyfill would clash with the library method of the same signature.
* netstandard2.0-only packages: `System.Text.Json`, `System.Net.Http.Json` (in each REST provider, which compiles in the shared HTTP layer since 2026-10-29, [ADR-0006](0006-shared-http-layer.md)), `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode` and `System.Diagnostics.DiagnosticSource` (the last only in `OpenUrzednik.Diagnostics`, [ADR-0004](0004-dependency-policy.md)).
* Tests target `net472`, the oldest .NET Framework xUnit 4 supports, and run on Windows in the `test (net472)` CI job. Running there found real differences: a `null` `HttpResponseMessage.Content` (now handled), `HttpClient.Timeout` without an inner `TimeoutException` (the executor now measures elapsed time instead), Windows-only time zone ids, and hierarchical `Activity` ids.
* (Superseded on 2026-10-29, see below.) Because the API differs per target, a library that builds on OpenUrzednik.Nbp should multi-target too. One that ships netstandard2.0 alone compiles against the `DateTime` methods, and in a .NET 8+ app, where NuGet picks our `net8.0` build with `DateOnly`, it fails with `MissingMethodException`.
* Package validation (`EnablePackageValidation`, the Confirmation above) is not enabled yet; backlog item 21 enables it after the first stable release, with suppressions recording the intended per-target date API (superseded on 2026-10-29: no suppressions are needed).
* The test-only packages from dotnet/extensions 10.x (`Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.Extensions.Http.Resilience`) warn on every .NET Framework target; the tests suppress that warning. The libraries don't depend on them.

**2026-10-04 change: performance comes first.** Supporting an older target must not make the .NET targets slower. The difference goes into a polyfill when possible (a member with the .NET name, compiled only for the target that lacks it, so the call site is the same everywhere); otherwise into a narrow `#if NET`. A slower shared version of fast .NET code is not acceptable. The .NET performance analyzers (CA18xx) are build warnings in `src/`, and errors in CI.

The support window was the open question while this ADR was `proposed`. Options were following Microsoft's lifecycle exactly (dropping `net8.0` in November 2026) or keeping a grace period. On 2026-10-02 the maintainer chose a 6-month grace period, so consumers have time to upgrade after Microsoft ends support, with the removal itself in a major release.

**2026-10-17 change: LTS targets and a .NET Framework floor** ([#83](https://github.com/milten89/OpenUrzednik.NET/issues/83)). `net9.0` is removed: .NET 9 apps now get the `net8.0` build, whose API is the same, so nothing breaks. The old rule ("all supported .NET versions", each removable only in a major six months after its end of support) would have kept an STS target alive for a year after Microsoft dropped it and turned each removal into a major release. The new rule targets the LTS releases, lets a non-lowest target go in a minor version, and keeps the lowest .NET target and netstandard2.0 for majors. The .NET Framework floor is stated as 4.7.2 (it was "4.6.2+/4.7.2+"), with the test approach above.

**2026-10-29 change: one public API for every build** ([#88](https://github.com/milten89/OpenUrzednik.NET/issues/88)). Before, public dates were `DateOnly` on .NET and `DateTime` on netstandard2.0, so the same interface had different signatures per build. A library built for netstandard2.0 alone compiled against the `DateTime` methods and, in a .NET 8+ app where NuGet picks the `net8.0` build, failed with `MissingMethodException`; package validation reported 52 differences between the builds. The maintainer chose `DateTime` everywhere, because the main audience runs .NET Framework and ASP.NET Core applications that often share one netstandard2.0 library. The title, the drivers, the chosen option (which now takes its public dates from "Uniform API"), the decision bullet, consequences and Confirmation were rewritten; the 2026-10-02 note's bullets on the per-target date API and the planned suppressions describe the state before this change. Callers that mocked a client with `Arg.Any<DateOnly>()` now set up the `DateTime` method (the call compiles against the extension, but no longer matches), and positional deconstruction of a model into a `DateOnly` no longer compiles. Model dates are now `DateTime`, so `ToString()` includes a time and System.Text.Json writes them as `"2026-10-02T00:00:00"` instead of `"2026-10-02"`, which changes the JSON of any app that returns the models (the Web API sample included).
