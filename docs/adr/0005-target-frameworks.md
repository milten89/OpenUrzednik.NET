---
status: accepted
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0005. Target frameworks and one public API

## Context and Problem Statement

Many companies integrating with Polish public APIs still run .NET Framework applications. Modern .NET offers better types (`DateOnly`, `TimeProvider`, generated regexes) that we want to use. Which target frameworks do we support, for how long, and how do we deal with APIs missing on older targets?

## Decision Drivers

* Reach .NET Framework 4.7.2+ consumers.
* Use modern types inside the library on modern .NET without slowing it down: supporting an older target must not make the .NET targets slower.
* One public API for every build, so a library built for netstandard2.0 on top of ours works in .NET Framework and .NET apps alike. The main audience runs .NET Framework and ASP.NET Core applications that often share one netstandard2.0 library.
* No extra dependencies on .NET targets ([ADR-0004](0004-dependency-policy.md)).
* On .NET Framework a strong-named app or library can reference only strong-named assemblies.

## Considered Options

* netstandard2.0 + the supported .NET LTS releases, `#if` for differences
* Modern .NET only
* Uniform API (`DateTime` everywhere or a custom date struct)
* Per-target public dates (`DateOnly` on .NET, `DateTime` on netstandard2.0)

For the support window of a .NET target:

* LTS releases; a target that isn't the lowest one removed in a minor version once Microsoft has ended its support; the lowest .NET target and netstandard2.0 removed only in a major version
* Every supported .NET version, each removed only in a major version six months after Microsoft ends its support
* Microsoft's support lifecycle exactly

## Decision Outcome

Chosen option: "netstandard2.0 + the supported .NET LTS releases, `#if` for differences", with the public dates taken from "Uniform API (`DateTime` everywhere…)": modern types stay inside the library and in .NET-only additions. For the support window: "LTS releases; …", because it doesn't keep an STS target alive after Microsoft drops it or turn each removal into a major release, and an app loses its build only in a major.

* **Target frameworks:** netstandard2.0 plus the .NET LTS releases in support (`netstandard2.0;net8.0;net10.0`; the .NET targets are in the root `Directory.Build.props`). A newer target (an STS release) is added only when it brings APIs we use. Apps on a .NET version without its own target get the nearest lower one (a .NET 9 app gets `net8.0`).
* **.NET Framework: 4.7.2 or later**, through netstandard2.0. On 4.6.1–4.7.1, netstandard2.0 libraries need extra facade assemblies (about 100 on 4.6.1–4.7.0) or binding redirects; Microsoft recommends 4.7.2 or later.
* **One public API for every build:** every build has the same public signatures for what it shares; the .NET builds may add members, never change them. Public dates are `DateTime` on every target, in parameters and model properties: a method converts its date arguments with `DateOnly.FromDateTime` at the start (the time of day and `Kind` are ignored), and returned dates are `00:00` with `Kind = Unspecified` (`ToPublicDate()` in the mappers). On .NET 8 and later, `DateOnly` overloads come as .NET-only extension methods (`NbpDateOnlyExtensions`), never as interface members.
* **Dates inside the library:** the code says `DateOnly` on every target. On netstandard2.0 a global using makes it an alias for `DateTime`, and `Nbp/Polyfills/DateOnlyPolyfills.cs` adds the `DateOnly` members the code needs (e.g. `FromDateTime`, `DayNumber`) as C# 14 extension members. Dates are compared by `DayNumber`, so a time of day is ignored rather than rejected.
* **Differences between targets, performance first:** a difference goes into a polyfill when possible: a member with the .NET name, compiled only for the target that lacks it, so the call site is the same everywhere. Otherwise it goes into a narrow `#if NET`. Where a polyfill would clash with a library method of the same signature, a small internal helper holds the `#if` (e.g. the bounded JSON body read in `src/Http/Infrastructure/HttpContentJson.cs`). `#if` stays inside such helpers, not spread through business code. A slower shared version of fast .NET code is not acceptable.
* **Polyfills** are internal, compiled only for netstandard2.0, one copy per assembly: shared ones in `src/Polyfills`, linked into every library (e.g. compiler and nullable attributes, the `ArgumentNullException.ThrowIfNull` family, `Enum.IsDefined<T>`, `string.Create(IFormatProvider, …)`, `string.EndsWith(char)`), the HTTP ones in `src/Http/Polyfills`, and those of one package in its own `Polyfills` folder (e.g. the span `StringBuilder.Append`/`string.Concat` overloads in `OpenUrzednik.Extensions.Logging`).
* **netstandard2.0-only dependencies** are limited to official Microsoft BCL packages; the approved ones are the allow-lists of [ADR-0004](0004-dependency-policy.md).
* **Performance analyzers:** every .NET performance rule (CA18xx) is a build warning in `src/` (`AnalysisModePerformance`), and an error in CI. A warning is fixed; it is suppressed only in code, at the place it fires, with a justification saying why the analyzer is wrong there. The paths where performance matters most (hot paths) and the working rules for them are listed in [`CLAUDE.md`](../../CLAUDE.md#performance), which changes as the code grows.
* **Strong naming:** every assembly is strong-named with one key, `OpenUrzednik.snk` in the repository root (public key token `e8d5ca0d09eb5ce1`), following Microsoft's [strong-naming guidance](https://learn.microsoft.com/dotnet/standard/library-guidance/strong-naming) for open-source libraries that target .NET Standard or .NET Framework.
  * The key pair (public and private) is checked in, as the guidance recommends: a strong name is an identity, not a security boundary, so anyone may rebuild the libraries with it. Publisher identity is a matter for NuGet package signing.
  * The key is never removed or changed, because that changes the identity of every assembly (a binary breaking change, [ADR-0011](0011-compatibility-policy.md)).
  * `SignAssembly` is set in the root `Directory.Build.props`, so every assembly is signed: the libraries, `OpenUrzednik.TestCommon`, every test project (`OpenUrzednik.IntegrationTests` included) and the samples. The samples don't need it, but one rule for every assembly is simpler. Test projects must be signed: a signed assembly grants `InternalsVisibleTo` only to signed friends, named with their full public key, and on .NET Framework a signed assembly loads only signed assemblies.
  * The `PublicKey` property gives every `InternalsVisibleTo` the key; a third-party friend carries its own key (`DynamicProxyGenAssembly2`, NSubstitute's proxies of internal types, carries Castle's public key).
  * A test dependency that test code references directly must be strong-named (otherwise CS8002); if it isn't, an agent stops and asks the maintainer. Unsigned third-party assemblies that our code doesn't reference directly (e.g. parts of WireMock.Net, the VSTest adapter) cause no CS8002 warning.
  * The assembly version changes only with the major version (NBGV `assemblyVersion.precision: major`; `1.0.0.0` for every 1.x, `0.0.0.0` for the 0.x previews), so .NET Framework apps rarely need new binding redirects. The file and informational versions carry the full version.
* **Testing:** every TFM is built and tested in CI. netstandard2.0 is tested by compiling the tests for `net472`, the oldest .NET Framework xUnit 4 supports, which proves no newer .NET Framework API is used, and running them on the Windows runner's .NET Framework 4.8.x (the `test (net472)` CI job). A machine has exactly one 4.x runtime and 4.8 is a compatibility update of 4.7.2, so a separate 4.7.2 run is neither needed nor possible on GitHub runners.
* **Package validation** (`EnablePackageValidation` in `src/Directory.Build.props`) is on for every package: each pack checks that the builds of a package are compatible with each other (e.g. CP0001 for a type only in netstandard2.0), without suppressions. From the first stable release, it also compares each package with the latest release (`PackageValidationBaselineVersion`, [ADR-0011](0011-compatibility-policy.md)); before that, previews may break their API.
* **Support window:**
  * A **.NET target that isn't the lowest one** may be removed in a minor version, once Microsoft has ended its support: apps on that version get the next lower .NET target, with the same API.
  * The **lowest .NET target** (`net8.0` at the time of writing) and **netstandard2.0** are removed only in a major version, because apps would then get a different build or none ([ADR-0008](0008-branching-versioning-and-release.md) allows breaking changes only in a major). netstandard2.0 is a specification, not a runtime, so it has no end of support; through it the library runs on .NET Framework 4.8, which Microsoft supports as part of Windows.

### Consequences

* Good, because the library is usable from legacy enterprise code.
* Good, because one netstandard2.0 library built on these packages works in .NET Framework and .NET apps alike, and package validation needs no suppressions.
* Good, because with polyfills the call sites are the same on every target and the .NET code stays as fast.
* Good, because a strong-named app or library on .NET Framework can use the packages.
* Bad, because .NET callers see `DateTime` in models and interfaces, where `DateOnly` would say more; the `DateOnly` overloads cover only the method parameters.
* Bad, because netstandard2.0 testing requires a Windows CI job. It catches .NET Framework differences the code must handle, e.g. a `null` `HttpResponseMessage.Content`, `HttpClient.Timeout` without an inner `TimeoutException`, Windows-only time zone ids and hierarchical `Activity` ids.
* Bad, because test-only packages from dotnet/extensions warn on every .NET Framework target; the tests suppress that warning, and the libraries don't depend on those packages.

### Confirmation

CI matrix includes all TFMs; package validation (`EnablePackageValidation` in `src/Directory.Build.props`) checks on every pack that the builds of a package are compatible with each other, without suppressions; CI packs `OpenUrzednik.Packages.slnf` in the `build (net10.0)` job on every PR, and the NuGet workflows pack before publishing. The performance analyzers fail the CI build (`CodeAnalysisTreatWarningsAsErrors`). The `reviewer` agent flags a public `DateOnly` and any interface member that exists only on .NET, and any change to the key, `SignAssembly` or the assembly-version precision. Before a release, the packed DLLs are checked for the public key token `e8d5ca0d09eb5ce1`.

## Pros and Cons of the Options

### Modern .NET only

* Bad, because .NET Framework applications, which many companies integrating with Polish public APIs still run, can't use the library.

### Per-target public dates (`DateOnly` on .NET, `DateTime` on netstandard2.0)

* Good, because .NET callers see `DateOnly` in models and interfaces.
* Bad, because the same interface has different signatures per build: a library built for netstandard2.0 alone compiles against the `DateTime` methods and, in a .NET 8+ app where NuGet picks the `net8.0` build, fails with `MissingMethodException`.

### Every supported .NET version, each removed only in a major version six months after Microsoft ends its support

* Good, because consumers have time to upgrade after Microsoft ends support.
* Bad, because it keeps an STS target alive for a year after Microsoft dropped it, and turns each removal into a major release.

### Microsoft's support lifecycle exactly

* Bad, because the lowest .NET target would also go when Microsoft ends its support (`net8.0` in November 2026), outside a major version, so an app on it loses its build in a minor update.

## More Information

* Follow-up: setting the package-validation baseline with the first stable release ([#117](https://github.com/milten89/OpenUrzednik.NET/issues/117)).

### Changes before 1.0

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0005-target-frameworks.md#more-information).

* 2026-10-02 · backlog item 17 · Implementation: `DateOnly` as an alias on netstandard2.0, netstandard2.0-only polyfills, `net472` tests; package validation planned.
* 2026-10-04 · Performance first: polyfills or a narrow `#if NET`, never a slower shared version; the CA18xx analyzers became build warnings in `src/` and errors in CI. The support window chosen on 2026-10-02 was recorded: a 6-month grace period after Microsoft ends support, with the removal in a major release.
* 2026-10-17 · [#83](https://github.com/milten89/OpenUrzednik.NET/issues/83) · The targets became netstandard2.0 plus the .NET LTS releases, with the support window above; the .NET Framework floor is 4.7.2.
* 2026-10-29 · [#88](https://github.com/milten89/OpenUrzednik.NET/issues/88) · One public API for every build: public dates are `DateTime` on every target.
* 2026-10-30 · [#93](https://github.com/milten89/OpenUrzednik.NET/issues/93) · Every assembly is strong-named with one checked-in key; the assembly version carries the major only.
* 2026-10-30 · [#94](https://github.com/milten89/OpenUrzednik.NET/issues/94) · Package validation is enabled for every package, without suppressions.
