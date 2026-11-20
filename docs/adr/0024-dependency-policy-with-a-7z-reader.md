---
status: accepted
date: 2026-11-20
confidence: medium
decision-makers: milten89
---

# 0024. Dependency policy, with a 7z reader for the White List flat-file package

## Context and Problem Statement

Libraries used by companies are judged by their dependency graph: every transitive package is a version conflict, a licence review and a potential vulnerability. At the same time consumers expect DI registration, `IHttpClientFactory` and resilience integration. Which dependencies may each package take, and which versions of them?

This record restates [ADR-0004](0004-dependency-policy.md), released in v1.0.0, and supersedes it. The one change: the Ministry of Finance publishes the VAT White List daily as a flat file, a 7z archive, and .NET can't read 7z (the BCL reads ZIP, Deflate, ZLib, GZip and Brotli in `System.IO.Compression`, and TAR in `System.Formats.Tar`). The planned package `OpenUrzednik.MF.WhiteList.FlatFile` ([ADR-0016](0016-next-providers-and-package-naming.md), [#220](https://github.com/milten89/OpenUrzednik.NET/issues/220)) needs a 7z reader, which ADR-0004's table didn't allow a provider package. Which reader does it use, and under which rules?

## Decision Drivers

* Minimal, predictable dependency graph for Core and provider packages.
* netstandard2.0 support ([ADR-0005](0005-target-frameworks.md)) needs some out-of-band BCL packages.
* First-class experience for consumers using `Microsoft.Extensions.*`.
* A version a published library depends on is a minimum for every app that uses it.
* For the 7z reader: an MIT-compatible licence, strong naming ([ADR-0005](0005-target-frameworks.md)), trimming and Native AOT safety on the .NET targets ([ADR-0014](0014-trimming-and-native-aot.md)), no native binaries, and support for what the flat file uses (LZMA2 data, an LZMA-packed header, below).

## Considered Options

* Microsoft.Extensions abstractions allowed everywhere; DI inside provider packages
* No dependencies in Core/providers; DI and integrations in separate packages
* Zero dependencies everywhere, no DI support

Within the chosen option:

* Resilience: DI packages add a default resilience handler, or the app adds its own
* Marking resilience rejections: an `Exception.Data` entry, or a marker type
* The rejection handler for clients the app registers: an extension named after the provider, a generic name, a shared package, or a public handler class
* Several setups of one provider: keyed services, or named registrations behind a public client factory; a `string` or an `object` key
* The version of `System.Diagnostics.DiagnosticSource`: the latest patch, as an implementation, or the oldest supported version
* The flat file's 7z reader: SharpCompress, an own reader as shared source, the app unpacking the archive itself, a wrapper of the native 7-Zip library, the `7z` executable, or Aspose.Zip

## Decision Outcome

Chosen option: "No dependencies in Core/providers; DI and integrations in separate packages". The flat-file package's 7z reader is SharpCompress (below, **7z reader**), the only maintained managed reader of LZMA2 7z archives with an MIT licence, strong naming and AOT annotations.

| Package kind | Examples | Allowed dependencies |
|---|---|---|
| Core | `OpenUrzednik.Core` | None on .NET targets. On netstandard2.0 only official Microsoft out-of-band BCL packages (e.g. `System.Text.Json`, `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode`). |
| Shared source (not a package) | `src/Http` ([ADR-0006](0006-shared-http-layer.md)) | None of its own: it is compiled into each REST provider, which references what it needs on netstandard2.0 (`System.Net.Http.Json`, `System.Text.Json`, `Microsoft.Bcl.TimeProvider`). |
| Provider | `OpenUrzednik.Nbp` | Same as Core + other OpenUrzednik packages. |
| Provider variant with an approved third-party dependency | `OpenUrzednik.MF.WhiteList.FlatFile` | Same as Provider + the package this record approves for it: `SharpCompress` (below, **7z reader**). Any other third-party package needs its own ADR. |
| Integration | `OpenUrzednik.Nbp.DependencyInjection`, `OpenUrzednik.Extensions.Logging` | `Microsoft.Extensions.*` (DI, Options, Http, Http.Resilience, Logging) and the package they integrate. DI packages may also use `Polly.Core`, which `Microsoft.Extensions.Http.Resilience` brings in, to recognise rejections (below). |
| Tracing adapter | `OpenUrzednik.Diagnostics` | `System.Diagnostics.DiagnosticSource` on netstandard2.0 only (`ActivitySource` is built into .NET). Core, the shared HTTP layer and the providers don't reference it ([ADR-0003](0003-telemetry-abstractions.md)). |
| Tests | `tests/*` | Anything appropriate. |

* Build-only tooling (`Nerdbank.GitVersioning`, analyzers) uses `PrivateAssets="all"` and never becomes a package dependency.
* Versions are managed centrally in `Directory.Packages.props` (central package management); a `PackageReference` carries no version. netstandard2.0-only references use a `Condition` on `TargetFramework`.
* A new `PackageReference` in a `src/` project needs an ADR or the maintainer's approval, also for a package the table allows; only then is it added to that project's allow-list (Confirmation), never just to make a build pass.

### Dependency versions

The rule covers only the dependencies of the published libraries, which become minimums for the apps. Tests, build tooling and anything used only inside the repository take the latest version.

* **Abstractions** (API contracts under semantic versioning, e.g. `Microsoft.Extensions.Logging.Abstractions`) take the lowest version: the first release (`x.0.0`) of the major that matches the oldest .NET target (8.0.0 while that target is `net8.0`). A higher one would force upgrades on every app for no gain. A later patch is taken only if that version has a known vulnerability (the first fixed patch, raised by hand); the minimum rises, by hand, when the oldest .NET target is removed ([ADR-0005](0005-target-frameworks.md)).
* Any change that raises an abstraction above its minimum (typically a Dependabot PR) needs the maintainer's explicit answer first; agents stop and ask ([CLAUDE.md](../../CLAUDE.md)).
* **Implementations** (code that runs, e.g. `System.Text.Json`, `Microsoft.Extensions.Http`) take the latest patch of the chosen minor, so the apps get bug and security fixes. A minor or major raise is a deliberate change, made by hand. `Polly.Core` is an implementation: it is the resilience engine the app runs, even though the DI package only uses its exception types. `System.Diagnostics.DiagnosticSource` is an implementation too: on .NET Framework the package carries the `Activity` and `ActivitySource` code itself, so its fixes reach the app only through a newer version.
* **The DI package's `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options`** follow the major of each target, so a .NET 8 app isn't moved to `Microsoft.Extensions.*` a major ahead of its own framework: `net8.0` takes the latest 8.0.x patch, and `net10.0` and `netstandard2.0` the latest 10.0.x patch (the LTS line that still supports .NET Framework). The major of a published library's dependency follows our targets; a new major arrives together with a new target (ADR-0005).
* `Directory.Packages.props` groups the packages accordingly. A package with per-target versions has one normal `PackageVersion` row (the 10.0.x line) and a `net8.0`-only `PackageVersion Update` override (the 8.0.x line) in its own item group, never two conditional `Include` rows: Dependabot skips a package that has them. After .NET 8 reaches end of support (2026-11-10) the 8.0.x line gets no patches; the override then stays as it is until `net8.0` is removed in a major version (ADR-0005).
* **Dependabot** ([`.github/dependabot.yml`](../../.github/dependabot.yml)) ignores every version update of `*.Abstractions`; a vulnerability still shows as a Dependabot alert, which doesn't depend on that file. For the published libraries' other dependencies it proposes patch updates only, in one grouped PR; a vulnerability fixed only in a newer minor shows as an alert and is raised by hand. Test and tooling packages get every update. The `net8.0` overrides are patched by hand, and a Dependabot PR that moves a `net8.0` version off the 8.0.x line is corrected by hand before merging.

### 7z reader

The flat-file package reads the archive with [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT), and only that package references it.

* **The archive** (measured on `20261120.7z`, 248,926,546 bytes): one solid block of LZMA2 with a 32 MB dictionary, a header packed with LZMA, and two entries, `YYYYMMDD.json` (501,913,574 bytes unpacked) and `YYYYMMDD.json.sha512sum` (the JSON's SHA-512 in `sha512sum` format). SharpCompress reads both codecs.
* **The package:** strong-named, with no dependencies of its own on `net8.0` and `net10.0`, and `IsAotCompatible` and `IsTrimmable` set on those targets. On netstandard2.0 it brings `Microsoft.Bcl.AsyncInterfaces` and `System.Text.Encoding.CodePages`, both `[8.0.0, )`, which the netstandard2.0 rule above already allows.
* **Version:** it is an implementation, so it takes the latest patch of a chosen minor, today 0.50.x, and never less than 0.50.9, the first version with the fixes of the 7z reader advisories below. It is still 0.x and its minor versions break its API, so a minor raise is a deliberate change made by hand, checked against its release notes. A raise that lifts its netstandard2.0 dependencies above 8.0.0 (0.51.0 previews take `10.0.x`) is treated like raising an abstraction: the maintainer decides first.
* **Use:** the package calls only the archive read API (open the archive, list its entries, `OpenEntryStream()`), never its writers or `WriteToDirectory`, where its path-traversal advisories are. Its read path has had denial-of-service advisories too ([GHSA-5wf8-cxh5-32fv](https://github.com/adamhathcock/sharpcompress/security/advisories/GHSA-5wf8-cxh5-32fv), an endless loop on truncated LZMA2, fixed in 0.50.6; [GHSA-vwpg-q8vc-g6fg](https://github.com/adamhathcock/sharpcompress/security/advisories/GHSA-vwpg-q8vc-g6fg), huge allocations from 7z header fields, fixed in 0.50.9; [GHSA-7jcq-g98g-83v5](https://github.com/adamhathcock/sharpcompress/security/advisories/GHSA-7jcq-g98g-83v5), an oversized allocation from `NextHeaderSize`, no fixed version stated), so the package bounds what it accepts: the two expected entries, sizes no larger than a configured limit, and the caller's cancellation token. A 7z header points to the end of the file, so the reader needs a seekable stream: the package downloads the archive to a temporary file and reads from it.
* **Native AOT:** SharpCompress's own AOT smoke test covers ZIP only, so `tests/OpenUrzednik.AotSmoke` opens a small 7z fixture through the flat-file package.
* **.NET Framework:** SharpCompress's assembly version changes with every release, so an app on .NET Framework that resolves a newer SharpCompress than the one the package was built against needs a binding redirect; the package README says so.
* **Fallback:** if the 0.x changes become a burden, an own reader as shared source, built on the LZMA SDK's public-domain decoder (options below), replaces it in a later ADR.

Storing the unpacked data and looking hashes up in it (memory, files on disk, a database, an extension point for other stores) is a separate decision, made in its own ADR before that code.

### DI packages and resilience

* A DI package registers the provider's clients with validated options on a named `HttpClient` and returns the `IHttpClientBuilder`, through `services.AddOpenUrzednik<Provider>(...)` (e.g. `services.AddOpenUrzednikNbp(...)`). Options are checked at startup (`ValidateOnStart`) with the clients' own rules (`NbpOptions.Validate()`).
* An app with several setups of one provider (e.g. one direct and one through a gateway) registers each under a `string` service key, through `AddKeyedOpenUrzednik<Provider>(serviceKey, ...)` (e.g. `AddKeyedOpenUrzednikNbp(serviceKey, configure)`): the clients are keyed transient services, as Microsoft recommends for `HttpClient`s, with the key's own named `HttpClient` (`OpenUrzednik.Nbp:<key>`) and options named after the key, read once like the default ones. Keyed services need Microsoft's dependency injection 8.0 or later (or a container that supports them).
* Resilience follows the .NET standard ([Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)): the app opts in by chaining `AddStandardResilienceHandler(...)` (or `AddResilienceHandler(...)`) on the returned builder. DI packages don't add a resilience handler themselves, because the guidance is one handler per client, not stacked.
* A DI package references `Polly.Core` only to recognise the rejection types, not `Microsoft.Extensions.Http.Resilience` itself: apps that don't add a resilience handler don't get it.
* DI packages translate the exceptions a resilience handler throws when it rejects a request, so they don't escape the result ([ADR-0002](0002-result-pattern-and-error-handling.md)). The translating `DelegatingHandler` is internal and goes outermost (before any resilience handler), and only once. Each DI package exposes it as a public extension of `IHttpClientBuilder` named after the provider (e.g. `AddOpenUrzednikNbpRejectionHandler()`), which its own registration uses, so a client the app registers itself gets it too. The handler catches Polly's `ExecutionRejectedException` types and rethrows them as exceptions the request executor ([ADR-0006](0006-shared-http-layer.md)) already handles:
  * `TimeoutRejectedException` → `TaskCanceledException` whose inner exception is the rejection (not a `TimeoutException`), returned as a `RequestTimeoutError`. The executor reports a limit in `RequestTimeoutError.Timeout` only when it knows that limit elapsed: its own deadline cancelled the request, or at least `HttpClient.Timeout` went by. A resilience handler's timeout is reported without a limit, not as `HttpClient.Timeout`.
  * other rejections (open circuit breaker, rate limiter) → an `HttpRequestException` marked as a rejection with an `Exception.Data` entry (`ResilienceRejection` in the shared HTTP layer, which the DI package compiles in too), returned as a `ServiceUnavailableError`; the executor also recognises an exception that wraps a marked one, as .NET Framework's `HttpClient` may. An unmarked `HttpRequestException` is a network failure, returned as a `ConnectionFailedError`.
  * Caller cancellation still throws. The shared HTTP layer itself doesn't reference Polly.

### Consequences

* Good, because consumers not using Microsoft.Extensions pay nothing.
* Good, because conflicts with consumers' Microsoft.Extensions versions are limited to opt-in packages.
* Good, because resilience is the app's explicit choice, through the standard API.
* Bad, because there are more packages to version, publish and document.
* Bad, because some convenience (e.g. `IOptions<T>` in client constructors) is not available in provider packages; clients take plain option objects.
* Good, because the flat-file package reads the archive in managed code, with a maintained, strong-named, AOT-annotated library, and apps that don't use the flat file never get it.
* Bad, because SharpCompress is 0.x: its minor versions break its API, so an app that also gets it through another library at a different minor can fail at runtime (`MissingMethodException`), and every minor raise needs a check by hand.
* Bad, because its 7z decoding speed is unmeasured in our use; open issues report slow 7z decompression, so the implementation measures it on a real archive.

### Confirmation

The build checks every `src/` project's direct `PackageReference`s against an allow-list (`CheckPackageReferenceAllowList` in [`src/Directory.Build.targets`](../../src/Directory.Build.targets)): the default list there holds the approved Core and provider packages; the Diagnostics adapter and the integration packages name theirs in their csproj (`OpenUrzednikAllowedPackageReferences`). The lists hold the packages approved so far, a subset of what the table above allows, and any other reference fails the build. References the SDK adds itself and build-only ones (`PrivateAssets="all"`) aren't dependencies of the package and aren't checked; a runtime library referenced with `PrivateAssets="all"` would pass the check (and break the package at runtime), which code review catches. `dotnet list package --include-transitive` for each `src/` provider/Core project shows no non-BCL dependencies (the flat-file package: SharpCompress only); the `reviewer` agent flags new `PackageReference`s in Core/provider projects.

SharpCompress goes only into the flat-file package's own `OpenUrzednikAllowedPackageReferences`, added with that package; the `reviewer` agent flags it anywhere else, a call to its writers or `WriteToDirectory`, and a minor raise without a note on its release notes. The `aot (net10.0)` job opens a 7z fixture through the package.

## Pros and Cons of the Options

### DI packages add a default resilience handler

* Good, because a registration comes with resilience defaults.
* Bad, because an app adding its own handler would then stack two, which Microsoft's guidance advises against.

### A marker type for resilience rejections

* Bad, because it would need `InternalsVisibleTo` from the provider to the DI package, which also exposes the provider's internal polyfills and makes them clash with the DI package's own copy on netstandard2.0.

### A generic name, a shared package or a public class for the rejection handler

* Bad (generic name), because every DI package defines its own extension, so one name in `Microsoft.Extensions.DependencyInjection` would make calls ambiguous in an app that references two DI packages.
* Bad (shared package), because a package for one handler isn't worth a new dependency.
* Bad (public handler class), because placed inside a resilience handler instead of outside it would silently do nothing, which the extension rules out.

### Named registrations behind a public client factory (`INbpClientFactory`)

* Good, because it works with containers that lack keyed services.
* Bad, because it adds an interface frozen from 1.0 ([ADR-0011](0011-compatibility-policy.md)) and makes callers depend on the factory. A factory can still be added in a minor version.

### An `object` service key

* Good, because Microsoft's keyed APIs take one.
* Bad, because naming the options and the `HttpClient` after it would need a `ToString()`.

### The oldest supported version of `System.Diagnostics.DiagnosticSource`

* Good, because [Microsoft's library guidance](https://learn.microsoft.com/dotnet/standard/library-guidance/dependencies) suggests the oldest supported version for netstandard libraries.
* Bad, because on .NET Framework the package carries the `Activity` and `ActivitySource` code itself, so the app would not get its fixes.

### SharpCompress

* Good, because it is MIT, maintained (0.50.10 released 2026-11-18), strong-named, annotated for trimming and AOT, reads LZMA and LZMA2 7z archives from managed code, and has no dependencies of its own on the .NET targets.
* Bad, because it is 0.x with breaking minors, its AOT smoke test doesn't cover 7z, and its 7z reader has had denial-of-service advisories (allocations, endless loops), so we follow its advisories and keep its patch current.

### An own reader as shared source

A 7z container and LZMA2 reader in `src/`, built on the LZMA SDK's public-domain C# LZMA decoder (the SDK's C# code has no LZMA2 and no 7z container), tested and mutated through a test host ([ADR-0019](0019-shared-http-layer-tested-through-test-host.md)).

* Good, because it adds no dependency and we control its API.
* Bad, because it is roughly 1,500–2,500 lines of parser for untrusted binary input (oversized dictionaries, decompression bombs) that we would own, test to the mutation gate and keep fast. It stays the fallback.

### The app unpacks the archive itself

The package reads the unpacked JSON from a stream.

* Good, because the package has no dependency.
* Bad, because every app writes the download and unpacking itself, which breaks the one-line default ([ADR-0007](0007-client-api-and-extensibility.md)), and the checksum and entry checks move to the app.

### A wrapper of the native 7-Zip library, the `7z` executable, or Aspose.Zip

* Bad (native wrappers, such as SevenZipSharp), because they are LGPL, need a native binary per platform, and their interop isn't AOT-safe.
* Bad (the `7z` executable), because most servers and containers don't have it, finding it on `PATH` can run the wrong program, and it works through processes and files, not results.
* Bad (Aspose.Zip), because its licence is commercial.

## More Information

* Supersedes [ADR-0004](0004-dependency-policy.md); everything except the 7z reader is restated from it unchanged.
* The flat file's format and the measurements come from the [specification of 30.09.2026](https://www.gov.pl/web/kas/plik-plaski) and the archive of 20.11.2026; storage and lookup get their own ADR (an issue opened with this record).

### Changes before 1.0 (of ADR-0004)

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0004-dependency-policy.md#more-information).

* 2026-10-02 · DI packages add no resilience defaults: the app chains the standard handler, and the DI package returns its rejections as errors.
* 2026-10-02 · backlog item 15 · The DI package references `Polly.Core` only, turns a timeout rejection into a `TaskCanceledException`, and validates options at startup.
* 2026-10-04 · Dependency versions: abstractions take the lowest version, implementations the latest.
* 2026-10-04 · Dependabot ignores every version update of `*.Abstractions`.
* 2026-10-17 · [#77](https://github.com/milten89/OpenUrzednik.NET/issues/77) · `System.Diagnostics.DiagnosticSource` is referenced only by the Diagnostics adapter on netstandard2.0, as an implementation.
* 2026-10-17 · [#84](https://github.com/milten89/OpenUrzednik.NET/issues/84) · The DI package's `Microsoft.Extensions.*` follow the major of each target; Dependabot proposes no major updates of the published libraries' dependencies.
* 2026-10-29 · [#148](https://github.com/milten89/OpenUrzednik.NET/issues/148) · Implementations take the latest patch of the chosen minor, Dependabot proposes patches only, and per-target versions use one row plus a `net8.0` override.
* 2026-10-29 · [#85](https://github.com/milten89/OpenUrzednik.NET/issues/85) · The HTTP layer became shared source instead of a package.
* 2026-10-29 · [#90](https://github.com/milten89/OpenUrzednik.NET/issues/90) · The DI package marks circuit-breaker and rate-limiter rejections; an unmarked `HttpRequestException` is a network failure.
* 2026-10-30 · [#110](https://github.com/milten89/OpenUrzednik.NET/issues/110) · The build checks `PackageReference`s against an allow-list.
* 2026-11-05 · [#122](https://github.com/milten89/OpenUrzednik.NET/issues/122) · The rejection handler is a public extension named after the provider, for clients the app registers itself.
* 2026-11-05 · [#124](https://github.com/milten89/OpenUrzednik.NET/issues/124) · Keyed registration for several setups of one provider.
