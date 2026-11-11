---
paths:
  - "src/**"
  - "tests/**"
  - "**/*.props"
  - "**/*.targets"
  - "version.json"
---

# Target frameworks ([ADR-0005](../../docs/adr/0005-target-frameworks.md))

- Every assembly, tests included, is strong-named with `OpenUrzednik.snk` (root `Directory.Build.props`; the key pair is checked in on purpose, Microsoft's guidance for OSS).
  - Never change, replace or remove the key.
  - A new project is signed automatically; a new `InternalsVisibleTo` gets the key from the `PublicKey` property, except a third-party friend such as `DynamicProxyGenAssembly2`, which needs its own `Key`.
  - If test code references a new test dependency directly, that dependency must be strong-named (CS8002); if it isn't, stop and ask.
  - The assembly version changes only with the major (NBGV `assemblyVersion.precision: major`).
- Every build has the same public signatures for what it shares; the .NET builds may add members, never change them. Public dates are `DateTime` on every target (parameters and model properties): a method converts its arguments with `DateOnly.FromDateTime` at the start (time of day and `Kind` ignored), and mappers return `ToPublicDate()` (midnight, `DateTimeKind.Unspecified`). `DateOnly` overloads exist only as .NET-only extension methods (`NbpDateOnlyExtensions`), never as interface members.
- Internally, write `DateOnly`: on netstandard2.0 it is a global alias for `DateTime` (`src/Dates/Dates.props`, `tests/Directory.Build.props`), and `src/Dates/Polyfills/DateOnlyPolyfills.cs` adds the `DateOnly` members the code uses (`FromDateTime`, `DayNumber`). A provider with dates imports `src/Dates/Dates.props`: `ToPublicDate()`, `ToIso8601String()` and `WarsawCalendar.Today` (Polish "today", `TimeProvider`) come with it. Compare dates by `DayNumber`, which ignores a `DateTime`'s time of day.
- Missing BCL members are polyfilled for netstandard2.0 only: compiler and nullable attributes, the `ThrowIf*` guards, `Enum.IsDefined<T>`, `string.Create(IFormatProvider, …)` and `string.EndsWith(char)` (C# 14 extension members) in `src/Polyfills`, linked into every library; HTTP reads with a cancellation token and span `string.Concat` in `src/Http/Polyfills`; span `StringBuilder.Append`/`string.Concat` in `Extensions.Logging/Polyfills`.
  - Where a polyfill would clash with a library method of the same signature, a small helper holds the `#if` (`src/Http/Infrastructure/HttpContentJson.cs`).
  - Keep `#if` inside such helpers, not spread through business logic.
- .NET Framework differences seen so far: `HttpResponseMessage.Content` can be `null`; `HttpClient.Timeout` has no inner `TimeoutException`; time zones have Windows ids only (`Central European Standard Time`); `Activity` ids are hierarchical unless the app (or OpenTelemetry) switches to W3C.
