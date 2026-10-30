# OpenUrzednik.Diagnostics

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.pl.md)

Adapter that turns the spans of **OpenUrzednik.NET** clients into `Activity` objects from `System.Diagnostics`, which OpenTelemetry and other tracing tools collect.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Diagnostics
```

## Usage

Sources are named `OpenUrzednik.<Provider>` (e.g. `OpenUrzednik.Nbp`). In OpenTelemetry you can enable all of them at once:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("OpenUrzednik.*"));
```

On .NET 8 and later that's all you need: a client created without a trace source records its spans on its provider's source. On .NET Framework (`netstandard2.0`) a client records spans only when you pass a trace source:

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient,
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

`GetShared` creates one `ActivitySource` per name, which lives until the process ends; on .NET it is the same source the clients use by default. To use your own source, pass `new ActivityTraceSource(source)`. `AddOpenUrzednikNbp()` from OpenUrzednik.Nbp.DependencyInjection passes the shared source for you.

The sources have no version: the stability rules below apply instead.

## HTTP client spans

The request itself is a `Client` span from `HttpClient`, nested under `<provider>.http.get`. To record it, add the HTTP instrumentation as well:

- .NET 9 and later: `tracing.AddSource("System.Net.Http")`.
- .NET 8 and .NET Framework: `tracing.AddHttpClientInstrumentation()` from the `OpenTelemetry.Instrumentation.Http` package.

## What goes into the trace

- Each client method has a span `<provider>.<area>.<operation>` (e.g. `nbp.gold.latest`) with its parameters as tags (e.g. `nbp.top_count`).
- The HTTP request is a child span `nbp.http.get` with the tags `http.request.method`, `url.path` and `http.response.status_code`.
- A failure sets the `Error` status and OpenTelemetry's `error.type` tag to the error code (e.g. `serviceUnavailable`, `validation`; see the error table in the [OpenUrzednik.Core README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md#errors)). A span with several errors (validation) also gets an `error` event per error, and an exception adds an `exception` event.
- All spans are of kind `Internal`.

Each provider's README lists its span names and tags; for NBP see the [telemetry reference](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp/README.md#telemetry-reference).

### Stability

Within a major version, these don't change: source names, span names, the documented tags and their meaning, and `error.type` with its values (the error codes). A minor version may add spans and tags. The `error` and `exception` events and the status description are best-effort and may change in any version.

When nothing listens to the source, the adapter creates no `Activity` and allocates nothing.

On .NET Framework, `Activity` gets hierarchical ids by default, not W3C ones. OpenTelemetry switches the app to W3C; without it, set `Activity.DefaultIdFormat = ActivityIdFormat.W3C` and `Activity.ForceDefaultIdFormat = true`.

## Dependencies

On .NET the package has no dependencies, because `ActivitySource` is part of .NET. On `netstandard2.0` (.NET Framework) it depends on `System.Diagnostics.DiagnosticSource`.
