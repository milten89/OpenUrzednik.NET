---
status: proposed
date: 2026-10-01
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

* Target frameworks: `netstandard2.0;net8.0;net9.0;net10.0` (currently `net8.0;net9.0;net10.0`; netstandard2.0 to be added).
* Public date-only values are `DateOnly` on .NET and `DateTime` (`Kind = Unspecified`, time `00:00`) on netstandard2.0. The public API shape is therefore TFM-dependent; XML docs mention both.
* Differences are handled with preprocessor directives (`#if NET` / `#else`) as locally as possible – prefer small internal helpers/polyfills (e.g. `ThrowHelper` for `ArgumentNullException.ThrowIfNull`) over scattering `#if` through business code.
* netstandard2.0-only dependencies are limited to official Microsoft BCL packages (`System.Text.Json`, `Microsoft.Bcl.TimeProvider`, `System.Net.Http.Json` if needed).
* Every TFM is built and tested in CI. netstandard2.0 is tested by running tests on a .NET Framework TFM (`net472`/`net48`) on a Windows runner.

### Consequences

* Good, because the library is usable from legacy enterprise code.
* Good, because modern consumers keep idiomatic types.
* Bad, because the public API differs per TFM, which complicates docs and API compatibility checks.
* Bad, because netstandard2.0 testing requires a Windows CI job.

### Confirmation

CI matrix includes all TFMs; package validation (`EnablePackageValidation`) runs per TFM.

## More Information

**Open question (why this ADR is `proposed`):** support window for older .NET versions. Options: follow Microsoft's support lifecycle exactly (drop a TFM when it leaves support, e.g. net8.0 in November 2026), or keep it for a grace period. Decide before adding netstandard2.0 or before the first stable release, whichever comes first.
