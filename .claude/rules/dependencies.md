---
paths:
  - "**/*.csproj"
  - "**/*.props"
  - "**/*.targets"
  - ".github/dependabot.yml"
  - "src/*.DependencyInjection/**"
---

# Dependencies ([ADR-0004](../../docs/adr/0004-dependency-policy.md))

- On netstandard2.0, only official Microsoft BCL packages are allowed (`System.Text.Json`, `System.Net.Http.Json`, `Microsoft.Bcl.TimeProvider`, `Microsoft.Bcl.HashCode`), referenced in a `Condition="'$(TargetFramework)' == 'netstandard2.0'"` item group.
- `Microsoft.Extensions.*` is allowed only in integration packages (`*.DependencyInjection`, `OpenUrzednik.Extensions.Logging`). The tracing adapter `OpenUrzednik.Diagnostics` may depend only on `System.Diagnostics.DiagnosticSource`, and only on netstandard2.0.
- DI packages don't add a resilience handler: they return the `IHttpClientBuilder`, the app chains `AddStandardResilienceHandler()`, and the DI package turns the handler's rejections into errors. A client the app registers itself gets the same translation from the public `AddOpenUrzednik<Provider>RejectionHandler()` (named after the provider, so two DI packages don't clash).
- Versions live in `Directory.Packages.props` (central package management). Never put `Version=` on a `PackageReference`.
- Version choice applies to the **dependencies of the published libraries** only; tests, build tooling and anything used only inside the repository take the latest version.
  - For those dependencies: **abstractions** (API contracts under semantic versioning, e.g. `Microsoft.Extensions.Logging.Abstractions`) use the **lowest** version: the first release (`x.0.0`) of the major that matches our oldest .NET target (currently 8.0.0), because it is a minimum for every app.
  - Move to a later patch only if that version has a known vulnerability; the minimum rises when the oldest .NET target is removed (ADR-0005).
  - **Implementations** (code that runs, e.g. `System.Text.Json`, `Microsoft.Extensions.Http`) use the **latest patch** of their chosen minor, to get bug and security fixes; a minor or major raise is a deliberate change.
  - `Directory.Packages.props` follows the split, and Dependabot ignores version updates of `*.Abstractions`, so watch Dependabot alerts for them.
  - The DI package's `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options` take the latest patch of the major matching the target: a normal row with 10.0.x (`net10.0`, `netstandard2.0`) and a `net8.0`-only `PackageVersion Update` override with 8.0.x, so a .NET 8 app stays on `Microsoft.Extensions.*` 8.x.
  - Don't put two conditional `Include` rows for one package: Dependabot skips such packages.
  - Dependabot proposes only patch updates for the published libraries' dependencies, in one group (listed in `.github/dependabot.yml`); the `net8.0` overrides are patched by hand.
