# OpenUrzednik.Diagnostics

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by Narodowy Bank Polski (NBP) or any other Polish public institution.

Adapters that turn the spans of **OpenUrzednik.NET** clients into `Activity` objects from `System.Diagnostics` and record their metrics on a `Meter`, which OpenTelemetry and other tools collect.

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

On .NET 8 and later that's all you need: a client created without a trace source records its spans on its provider's source. On .NET Framework (`netstandard2.0`) a client records spans only when you pass a trace source in its `OpenUrzednikTelemetry`:

```csharp
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient, options: null, timeProvider: null, telemetry: new OpenUrzednikTelemetry
{
    TraceSource = ActivityTraceSource.GetShared(NbpTelemetry.SourceName),
});
```

`GetShared` creates one `ActivitySource` per name, which lives until the process ends; on .NET it is the same source the clients use by default. To use your own source, pass `new ActivityTraceSource(source)`. `AddOpenUrzednikNbp()` from OpenUrzednik.Nbp.DependencyInjection passes the shared source for you.

The sources have no version: the stability rules below apply instead.

## HTTP client spans

The request itself is a `Client` span from `HttpClient`, nested under `<provider>.http.get`. To record it, add the HTTP instrumentation as well:

- .NET 9 and later: `tracing.AddSource("System.Net.Http")`.
- .NET 8 and .NET Framework: `tracing.AddHttpClientInstrumentation()` from the `OpenTelemetry.Instrumentation.Http` package.

## What goes into the trace

- Each client method has a span `<provider>.<area>.<operation>` (e.g. `nbp.gold.latest`) with its parameters as tags (e.g. `nbp.top_count`).
- The HTTP request is a child span `nbp.http.get` with the tags `http.request.method`, `server.address`, `server.port`, `url.path` and `http.response.status_code` (OpenTelemetry's HTTP client conventions).
- A failure sets the `Error` status and OpenTelemetry's `error.type` tag to the error code, or to `_OTHER` when a client method threw (a cancelled call included) (e.g. `serviceUnavailable`, `validation`; see the error table in the [OpenUrzednik.Core README](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Core/README.md#errors)). A span with several errors (validation) also gets an `error` event per error, and an exception adds an `exception` event.
- All spans are of kind `Internal`.

Each provider's README lists its span names and tags; for NBP see the [telemetry reference](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Nbp/README.md#telemetry-reference).

### Stability

Within a major version, these don't change: source names, span names, the documented tags and their meaning, `error.type` with its values (the error codes), and that a client writes an operation's log entries while that operation's span (or its `nbp.http.get` child) is `Activity.Current` ([below](#correlating-logs-and-traces), [ADR-0015](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0015-log-and-trace-correlation.md)). A minor version may add spans and tags. The `error` and `exception` events and the status description are best-effort and may change in any version.

When nothing listens to the source, the adapter creates no `Activity` and allocates nothing.

On .NET Framework, `Activity` gets hierarchical ids by default, not W3C ones. OpenTelemetry switches the app to W3C; without it, set `Activity.DefaultIdFormat = ActivityIdFormat.W3C` and `Activity.ForceDefaultIdFormat = true`.

## Correlating logs and traces

A client writes its log entries while the operation's span is `Activity.Current`: entries about the request and its response (HTTP errors, timeouts, connection failures) while its `nbp.http.get` child span is, the others (validation, an empty answer) while the operation span is. So a log entry can carry the operation's trace and span ids, and your tool can show it next to the trace. The library doesn't add the ids itself; your logging setup does:

- **OpenTelemetry:** enable the sources for traces and add OpenTelemetry logging. Its log records take the trace and span ids from `Activity.Current`.

  ```csharp
  builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource("OpenUrzednik.*"));
  builder.Logging.AddOpenTelemetry();
  ```

- **Plain `Microsoft.Extensions.Logging`:** add the ids to every entry's scope, and use a provider that writes scopes (e.g. the console with `IncludeScopes`):

  ```csharp
  builder.Logging.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
  ```

The ids are there only when a span exists: something must listen to the `OpenUrzednik.<Provider>` source (above), and on .NET Framework the client needs a trace source from this package and the W3C id format. Without a span, entries carry the ids of whatever activity was current before the call (e.g. the incoming ASP.NET Core request), if any. The logs come through [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging); a logger of your own (`IOpenUrzednikLogger`) has to read `Activity.Current` itself.

## Metrics

Each client method records its duration on a meter named like its provider's source, `OpenUrzednik.<Provider>` (e.g. `OpenUrzednik.Nbp`). In OpenTelemetry you can enable all of them at once:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("OpenUrzednik.*"));
```

On .NET 8 and later that's all you need: a client created without metrics records on its provider's meter, which costs nothing while nothing listens. On .NET Framework (`netstandard2.0`) a client records metrics only when you pass `MeterMetrics` from this package in its `OpenUrzednikTelemetry`:

```csharp
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;

var client = new NbpGoldPriceClient(httpClient, options: null, timeProvider: null, telemetry: new OpenUrzednikTelemetry
{
    Metrics = MeterMetrics.GetShared(NbpTelemetry.SourceName),
});
```

`GetShared` returns one instance per meter name for the whole process. On .NET it records on the same meter the clients use by default; `MeterMetrics.Meter` is that meter.

### With dependency injection

`AddOpenUrzednikNbp()` from OpenUrzednik.Nbp.DependencyInjection passes metrics for you, on every target. When the container has an `IMeterFactory`, its clients record on a meter of that factory (`MeterMetrics.Create(meterFactory, name)`), which is disposed with the container. On .NET 8 and later `AddHttpClient()` registers a factory, and on .NET Framework the Generic Host or `services.AddMetrics()` does; without one, the clients record on the shared meter. This is what `HttpClient` does with its own metrics.

The factory meter has the same name, so `AddMeter("OpenUrzednik.*")` collects it, and OpenTelemetry merges its series with the shared meter's when an app also creates clients without DI. Your tests can collect one container's measurements by the factory, even when tests run in parallel:

```csharp
using Microsoft.Extensions.Diagnostics.Metrics.Testing; // Microsoft.Extensions.Diagnostics.Testing package

var meterFactory = provider.GetRequiredService<IMeterFactory>();
using var collector = new MetricCollector<double>(meterFactory, NbpTelemetry.SourceName, "openurzednik.client.operation.duration");

await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync();

var measurement = collector.GetMeasurementSnapshot().Single();
```

### What gets measured

| Instrument | Kind, unit | Tags |
|---|---|---|
| `openurzednik.client.operation.duration` | histogram, seconds (`s`) | `openurzednik.operation` (the operation's span name, e.g. `nbp.gold.latest`), `openurzednik.provider` (e.g. `nbp`), `error.type` (only on failure), `server.address`, `server.port` |

- Each client method call records one measurement when it ends, while its span is still `Activity.Current`, so an exemplar can link the measurement to its trace.
- When the operation is traced, the duration equals the span's; otherwise the client measures it once with its `TimeProvider`.
- `error.type` is the first error's code, as on the span, or `_OTHER` when the method threw (e.g. because the caller cancelled it).
- The histogram suggests OpenTelemetry's bucket boundaries for RPC calls (0.1 ms to 60 s) on .NET 10 and .NET Framework. On .NET 8, whose built-in `System.Diagnostics.DiagnosticSource` has no `InstrumentAdvice`, the listener's defaults apply; an OpenTelemetry view can set the boundaries on any target.
- Tags never carry personal identifiers or secrets.

Each provider's README lists its instruments; for NBP see the [telemetry reference](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Nbp/README.md#metrics).

Within a major version, meter names, instrument names, units and the documented tags with their meaning don't change. A minor version may add instruments and tags.

### Your own metrics sink

To send the measurements somewhere other than a `Meter`, implement `IOpenUrzednikMetrics` from OpenUrzednik.Core and pass it as `Metrics`. `Record` gets each measurement with its name, unit, description and tags, plus an interface for its kind of instrument. A sink that records by those values handles measurements added in later versions too:

```csharp
using System;
using System.Linq;

using OpenUrzednik.Core.Telemetry;

public sealed class ConsoleMetrics : IOpenUrzednikMetrics
{
    // Return false for names you don't record, so the clients skip measuring them.
    public bool IsEnabled(string measurementName) => true;

    public void Record(IOpenUrzednikMeasurement measurement)
    {
        // The measurement is valid only during this call: copy what you need, don't keep it.
        if (measurement is IOpenUrzednikHistogramMeasurement histogram)
        {
            var tags = string.Join(", ", measurement.Tags.Select(tag => $"{tag.Key}={tag.Value}"));
            Console.WriteLine($"{measurement.Name}: {histogram.Value} {measurement.Unit} [{tags}]");
        }

        // Ignore kinds this sink doesn't know.
    }
}
```

- Many clients call one instance at the same time, so make it thread-safe.
- A minor version may add a new kind of instrument (a counter or a gauge, for example) as a new measurement interface. Your sink records nothing of that kind until you handle its interface; the release notes say so under **Upgrading**, and `MeterMetrics` supports the new kind in the same release.

## Trimming and Native AOT

The .NET builds (`net8.0`, `net10.0`) are trimming- and Native AOT-compatible: publishing an app with trimming or Native AOT gives no warning from this package. CI checks it on every change ([ADR-0014](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0014-trimming-and-native-aot.md)). The `netstandard2.0` build (.NET Framework) isn't covered.

## Dependencies

On .NET the package has no dependencies, because `ActivitySource` and `Meter` are part of .NET. On `netstandard2.0` (.NET Framework) it depends on `System.Diagnostics.DiagnosticSource`.
