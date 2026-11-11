---
paths:
  - "src/**/*.cs"
---

# Performance

- **Supporting an older target must not slow down the .NET targets.** Hide the difference in a **polyfill** when possible: a member with the .NET name, compiled only for the target that lacks it (`src/Polyfills`, `src/Http/Polyfills`, `src/Dates/Polyfills`, `Nbp/Polyfills`), so the call site is the same everywhere. Only when no polyfill fits, use `#if NET` in the code, around as little code as possible. Never replace the fast .NET code with a slower shared version.
- The performance analyzers (all CA18xx rules) are build warnings in `src/` on every target (`AnalysisModePerformance=All` in `src/Directory.Build.props`; netstandard2.0 sets `EnableNETAnalyzers`, `AnalysisLevel` and `WarningLevel` there, equal to the highest .NET target, because the SDK sets them only for .NET 5+), and in CI they fail the build (`CodeAnalysisTreatWarningsAsErrors`). Fix the warning. Suppress it only with `[SuppressMessage(..., Justification = "...")]` saying why the analyzer is wrong in that place.
- Hot paths, where this matters most: `RestRequestExecutor`, the NBP pipeline and mappers, `LogValues`/`OpenUrzednikLogger`, `ActivitySpan`, `OpenUrzednikResult`.
- The `reviewer` agent reports performance degradations in a section of their own; treat each like any other finding. The rules come from [ADR-0005](../../docs/adr/0005-target-frameworks.md) (target differences) and [ADR-0004](../../docs/adr/0004-dependency-policy.md) (versions).
