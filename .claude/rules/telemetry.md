---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Telemetry ([ADR-0003](../../docs/adr/0003-telemetry-abstractions.md))

- Each provider package has its own `ActivitySource`, named after the package id (`OpenUrzednik.Nbp`, `OpenUrzednik.MF.WhiteList`); the `<provider>` short name is the package id without `OpenUrzednik.`, in snake_case with `_` between segments (`nbp`, `mf_white_list`, `gus_regon`; [ADR-0016](../../docs/adr/0016-next-providers-and-package-naming.md)).
- Span names: `<provider>.<area>.<operation>` (e.g. `nbp.currency.buy_sell_latest`). Tags: `<provider>.<parameter>`; HTTP tags follow OpenTelemetry's HTTP client conventions (`http.request.method`, `server.address`, `server.port`, `url.path`, `http.response.status_code`). Record failures with `span.RecordError(s)`, which set OpenTelemetry's `error.type` to the error code; the `error` events are best-effort.
- Every log call passes an `OpenUrzednikEventId` (the one exception: the HttpClient logging of [ADR-0017](../../docs/adr/0017-http-client-logging-for-identifier-paths.md), which mirrors Microsoft's event ids under Microsoft's categories).
  - The shared HTTP layer's events are in `src/Http/Infrastructure/RestLogEvents.cs` (1xxx); a provider's own are 2xxx and listed, with the HTTP ones, as `public const int` in its `<Prefix>LogEvents` class (`NbpLogEvents`; the prefix is chosen in the provider ADR; the event name is the constant's name).
  - A new log event gets a new id: never renumber or reuse one.
  - The `{provider}` log property is the profile's short name (`nbp`), as in span names.
- Stable within a major: source names, span names, the documented tags, `error.type`, logger categories and event ids. Log message text and properties aren't.
- Log calls stay inside their span ([ADR-0015](../../docs/adr/0015-log-and-trace-correlation.md)): an operation's entries are written while its span (for the HTTP request and response, the `<provider>.http.get` child) is `Activity.Current`, so apps can link logs to traces. Never log after the span's `using` ends.
- Adapters: `OpenUrzednik.Extensions.Logging` (logger category = the client's full type name; the event id becomes `EventId`) and `OpenUrzednik.Diagnostics` (source `OpenUrzednik.<Provider>`, e.g. `NbpTelemetry.SourceName`; every span is `ActivityKind.Internal`).
- Guard `Debug` logs with `IsEnabled`, use message templates (no interpolation), and never put secrets (API keys, session ids, tokens) or personal identifiers (PESEL, NIP, REGON, bank account numbers) in log messages or properties, span names or tags, error and exception messages, error metadata or `Exception.Data`: show at most the last 4 characters, or use a template or display value.
  - A provider ADR may require stricter masking.
  - Mask with `SensitiveValue.Mask` (`src/Privacy`; a provider that handles identifiers or secrets imports `src/Privacy/Privacy.props`), never a provider's own helper unless its provider ADR requires stricter masking.
  - It isn't a Core internal: Core grants `InternalsVisibleTo` on the .NET targets only.
  - Core never imports `Privacy.props`: its copy and a provider's would clash (CS0436) through Core's `InternalsVisibleTo`.
