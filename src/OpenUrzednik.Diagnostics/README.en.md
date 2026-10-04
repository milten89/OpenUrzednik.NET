# OpenUrzednik.Diagnostics

🇵🇱 [Wersja polska](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Diagnostics/README.md)

Adapter that turns the spans of **OpenUrzednik.NET** clients into `Activity` objects from `System.Diagnostics`, which OpenTelemetry and other tracing tools collect.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.en.md) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.Diagnostics
```

## Usage

```csharp
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Gold;

var client = new NbpGoldPriceClient(httpClient,
    traceSource: ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
```

Sources are named `OpenUrzednik.<Provider>` (e.g. `OpenUrzednik.Nbp`). In OpenTelemetry you can enable all of them at once:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("OpenUrzednik.*"));
```

`GetShared` creates one `ActivitySource` per name, which lives until the process ends. To use your own source, pass `new ActivityTraceSource(source)`.

## What goes into the trace

- Each client method has a span `<provider>.<area>.<operation>` (e.g. `nbp.gold.latest`) with its parameters as tags (e.g. `nbp.top_count`).
- The HTTP request is a child span `nbp.http.get` with the tags `http.request.method`, `url.path` and `http.response.status_code`.
- An error sets the `Error` status and records the error code in `error.code`: as a span tag or in an `error` event. An exception adds an `exception` event.
- All spans are of kind `Internal`. The `Client` span for the request itself comes from the `HttpClient` instrumentation, nested under `nbp.http.get`.

When nothing listens to the source, the adapter creates no `Activity` and allocates nothing.

On .NET Framework, `Activity` gets hierarchical ids by default, not W3C ones. OpenTelemetry switches the app to W3C; without it, set `Activity.DefaultIdFormat = ActivityIdFormat.W3C` and `Activity.ForceDefaultIdFormat = true`.

## Dependencies

On .NET the package has no dependencies, because `ActivitySource` is part of .NET. On `netstandard2.0` (.NET Framework) it depends on `System.Diagnostics.DiagnosticSource`.
