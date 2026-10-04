---
status: accepted
date: 2026-10-02
decision-makers: milten89
---

# 0005. Target frameworks and per-TFM public API

## Context and Problem Statement

Many companies integrating with Polish public APIs still run .NET Framework applications. Modern .NET offers better types (`DateOnly`, `TimeProvider`, generated regexes) that we want to use. Which target frameworks do we support and how do we deal with APIs missing on older targets?

## Decision Drivers

* Reach .NET Framework 4.6.2+/4.7.2+ consumers.
* Use modern types on modern .NET without compromising their API.
* No extra dependencies on .NET targets ([ADR-0004](0004-dependency-policy.md)).

## Considered Options

* netstandard2.0 + all supported .NET versions, `#if` for differences
* Modern .NET only
* Uniform API (`DateTime` everywhere or a custom date struct)

## Decision Outcome

Chosen option: "netstandard2.0 + all supported .NET versions, `#if` for differences".

* Target frameworks: `netstandard2.0;net8.0;net9.0;net10.0`.
* Public date-only values are `DateOnly` on .NET and `DateTime` (`Kind = Unspecified`, time `00:00`) on netstandard2.0. The public API shape is therefore TFM-dependent; XML docs mention both.
* Differences are handled with preprocessor directives (`#if NET` / `#else`) as locally as possible – prefer small internal helpers/polyfills (e.g. `ThrowHelper` for `ArgumentNullException.ThrowIfNull`) over scattering `#if` through business code.
* netstandard2.0-only dependencies are limited to official Microsoft BCL packages (`System.Text.Json`, `Microsoft.Bcl.TimeProvider`, `System.Net.Http.Json` if needed).
* Every TFM is built and tested in CI. netstandard2.0 is tested by running tests on a .NET Framework TFM (`net472`/`net48`) on a Windows runner.
* Support window:
  * **.NET targets** (`net8.0`, `net9.0`, …) become removable **6 months after Microsoft ends their support**, and are removed in the **next major release** after that. Removing one is a breaking change: NuGet then gives those apps the netstandard2.0 build, whose dates are `DateTime` instead of `DateOnly` ([ADR-0008](0008-branching-versioning-and-release.md) allows breaking changes only in a major version). For example, `net8.0` and `net9.0` both reach end of support on 10 November 2026 (Microsoft extended STS releases to 24 months), so both become removable in May 2027.
  * **netstandard2.0** is kept. It is a specification, not a runtime, so it has no end of support; through it the library runs on .NET Framework 4.8, which Microsoft supports as part of Windows.

### Consequences

* Good, because the library is usable from legacy enterprise code.
* Good, because modern consumers keep idiomatic types.
* Bad, because the public API differs per TFM, which complicates docs and API compatibility checks.
* Bad, because netstandard2.0 testing requires a Windows CI job.

### Confirmation

CI matrix includes all TFMs; package validation (`EnablePackageValidation`) runs per TFM.

## More Information

**2026-10-02 implementation note** (backlog item 17):

* Dates: the code says `DateOnly` on every target. On netstandard2.0 a global using makes it an alias for `DateTime`, so the public API there takes and returns `DateTime`. `Nbp/Polyfills/DateOnlyPolyfills.cs` adds the `DateOnly` members the code needs as C# 14 extension members. Validators compare `DayNumber`, so a time of day is ignored rather than rejected.
* Polyfills are internal, compiled only for netstandard2.0, one copy per assembly: compiler attributes (init, required members, `CallerArgumentExpression`), nullable attributes the `ArgumentNullException.ThrowIfNull` family, `Enum.IsDefined<T>`, `string.Create(IFormatProvider, …)`, `string.EndsWith(char)` and, in `OpenUrzednik.Extensions.Logging`, the span `StringBuilder.Append`/`string.Concat` overloads (C# 14 extension members, so call sites don't change and the .NET code stays as fast as before). The JSON body read is bounded with `WaitAsync` on netstandard2.0 only, in a small `#if` helper, because a polyfill would clash with the library method of the same signature.
* netstandard2.0-only packages: `System.Text.Json`, `System.Net.Http.Json`, `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode` and `System.Diagnostics.DiagnosticSource`.
* Tests target `net472`, the oldest .NET Framework xUnit 4 supports, and run on Windows in the `test (net472)` CI job. Running there found real differences: a `null` `HttpResponseMessage.Content` (now handled), `HttpClient.Timeout` without an inner `TimeoutException` (the executor now measures elapsed time instead), Windows-only time zone ids, and hierarchical `Activity` ids.
* Because the API differs per target, a library that builds on OpenUrzednik.Nbp should multi-target too. One that ships netstandard2.0 alone compiles against the `DateTime` methods, and in a .NET 8+ app, where NuGet picks our `net8.0` build with `DateOnly`, it fails with `MissingMethodException`.
* Package validation (`EnablePackageValidation`, the Confirmation above) is not enabled yet; backlog item 21 enables it after the first stable release, with suppressions recording the intended per-target date API.
* The test-only packages from dotnet/extensions 10.x (`Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.Extensions.Http.Resilience`) warn on every .NET Framework target; the tests suppress that warning. The libraries don't depend on them.

**2026-10-04 change: performance comes first.** Supporting an older target must not make the .NET targets slower. The difference goes into a polyfill when possible (a member with the .NET name, compiled only for the target that lacks it, so the call site is the same everywhere); otherwise into a narrow `#if NET`. A slower shared version of fast .NET code is not acceptable. The .NET performance analyzers (CA18xx) are build warnings in `src/`, and errors in CI.

The support window was the open question while this ADR was `proposed`. Options were following Microsoft's lifecycle exactly (dropping `net8.0` in November 2026) or keeping a grace period. On 2026-10-02 the maintainer chose a 6-month grace period, so consumers have time to upgrade after Microsoft ends support, with the removal itself in a major release.
