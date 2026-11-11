---
paths:
  - "src/**/*.cs"
  - "samples/**/*.cs"
---

# Code conventions

- Namespaces ([ADR-0007](../../docs/adr/0007-client-api-and-extensibility.md)): clients, their interfaces and options in the provider's root namespace (`OpenUrzednik.Nbp`, files in the project root); models in area namespaces (`.Currency`, `.Table`, `.Gold`). Core: results and their `Task<…>` extensions in `OpenUrzednik.Core`, operations on a result as instance methods, extension methods next to the type they extend. No public `.Extensions` or `.Options` namespaces in Core or providers (package names like `OpenUrzednik.Extensions.Logging` and internal namespaces are exempt).
- Public models are `sealed record`s with `IReadOnlyList<T>` collections. Records holding lists override `Equals`/`GetHashCode` (see `Currency/CurrencyExchangeRates.cs`). New data on an existing model is a non-`required` `init` property, never a new positional parameter (it would change the constructor and `Deconstruct`); include it in a custom `Equals`/`GetHashCode` (ADR-0011).
- DTOs are `internal sealed class` in `Dto/`, with `[JsonPropertyName]`. Mark a property `required` **only if the real API always returns it**: check with `/verify-api`.
- Mappers are `internal static class Mapper` per area.
- Validators derive from `ValueValidator<T>` in `Validation/` and are `internal sealed`.
- Polish identifiers (NIP, REGON, KRS number, NRB) use the shared validators in `src/Identifiers`: a provider imports `Identifiers.props` (it brings `Privacy.props`; don't import both), throws `ArgumentNullException` for `null` first (ADR-0002), validates (spaces, hyphens and, for NIP and account numbers, a `PL` prefix are accepted) and sends the validator's `Normalized` digits; the errors carry the value masked.
  - Like `src/Privacy`, the files must not reach an assembly twice through `InternalsVisibleTo` (CS0436): a DI package or a test project that sees a provider's internals doesn't import them too.
- Public API needs `///` XML docs: a missing one is warning CS1591 in `src/`, and CI treats every warning as an error (`TreatWarningsAsErrors` when `ContinuousIntegrationBuild`; NuGet vulnerability warnings excepted). Mention API limits (date ranges, top count, publication schedule).
- Library code stays trimming- and Native AOT-safe: the .NET targets set `IsAotCompatible`, so reflection-based APIs show up as IL2xxx/IL3xxx warnings.
  - The CI job `aot (net10.0)` publishes `tests/OpenUrzednik.AotSmoke` with Native AOT, every published `src/` assembly rooted (`TrimmerRootAssembly`), so a warning in the libraries or their dependencies fails it; then it runs the binary, which calls the clients offline with captured fixtures.
  - A new published package gets a `ProjectReference` and a `TrimmerRootAssembly` there, and its dependencies and transport code must be AOT-safe (e.g. no reflection-based `XmlSerializer` for SOAP).
  - The READMEs promise this compatibility for the .NET builds of every package ([ADR-0014](../../docs/adr/0014-trimming-and-native-aot.md)).
- Large clients are split into `partial` files by area (`NbpCurrencyExchangeRateClient.BuySell.cs`).
- Messages and logs are in English. Format dates invariantly (`yyyy-MM-dd`).
- Library code (`src/`) awaits with `ConfigureAwait(false)`; CA2007 flags a missing one.
