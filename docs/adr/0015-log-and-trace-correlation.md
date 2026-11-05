---
status: accepted
date: 2026-11-06
confidence: high
decision-makers: milten89
---

# 0015. Log entries are written inside the operation's span

## Context and Problem Statement

The clients log through `IOpenUrzednikLogger` and trace through `IOpenUrzednikTraceSource` ([ADR-0003](0003-telemetry-abstractions.md)). Apps want to see a client's log entries next to the trace of the same operation. In .NET, log providers do that from `Activity.Current`: OpenTelemetry logging and `ActivityTrackingOptions` attach the trace and span ids of the current activity to each entry. The clients already write their entries while the operation's span is current, but nothing says so or keeps it that way. Is the link part of the telemetry contract, and how?

## Decision Drivers

* Linking logs to traces is what users of OpenTelemetry and similar tools expect from a .NET library.
* The library keeps no dependency on a logging or tracing implementation (ADR-0003, [ADR-0004](0004-dependency-policy.md)).
* A change that moved a log call outside its span would break the link without any error.

## Considered Options

* Ambient correlation through `Activity.Current`, as part of the telemetry contract
* Trace and span ids as properties of every log entry, added by the library
* No guarantee: the link works today, but isn't documented

## Decision Outcome

Chosen option: "Ambient correlation through `Activity.Current`, as part of the telemetry contract", because it is how .NET libraries link logs and traces, needs no code in the library, and works with any logging setup that reads `Activity.Current`.

* A client writes an operation's log entries while that operation's span is `Activity.Current`: entries about the HTTP request and its response while the child span `<provider>.http.get` is, the others while the operation's span is.
* It is stable within a major version, like the rest of the telemetry contract (ADR-0003, [ADR-0011](0011-compatibility-policy.md)).
* The library doesn't add the ids to log entries. The app's logging setup does (OpenTelemetry logging, or `ActivityTrackingOptions`), and the Diagnostics README shows both.
* The link needs a span: something must listen to the provider's source, and on .NET Framework the client needs a trace source from `OpenUrzednik.Diagnostics` and W3C activity ids.

### Consequences

* Good, because apps get correlated logs and traces by configuration alone.
* Good, because no log property, dependency or API is added.
* Bad, because entries have no ids when no span exists (nothing listens, or .NET Framework without a trace source).
* Bad, because a logger the app implements itself (`IOpenUrzednikLogger`) has to read `Activity.Current` on its own.

### Confirmation

`tests/OpenUrzednik.Extensions.Logging.Tests/NbpClientLogTraceCorrelationTest.cs` checks, through the real `ActivityTraceSource` and logging adapter, that each kind of entry (validation, empty answer, HTTP error, connection failure) is written while the expected span is `Activity.Current`, on .NET and .NET Framework. The `reviewer` agent flags a log call moved outside its span.

## Pros and Cons of the Options

### Trace and span ids as properties of every log entry

* Good, because the ids would be there even for loggers that don't read `Activity.Current`.
* Bad, because it duplicates what the logging stack already does, and adds properties to every entry.

### No guarantee

* Good, because nothing becomes part of the contract.
* Bad, because a later change could break the link silently, and users can't rely on it.

## More Information

Introduced in [#199](https://github.com/milten89/OpenUrzednik.NET/issues/199). The setup for apps is in the [OpenUrzednik.Diagnostics README](../../src/OpenUrzednik.Diagnostics/README.md#correlating-logs-and-traces). An accepted record isn't edited later ([ADR-0001](0001-record-architecture-decisions.md)): a changed decision gets a new record that supersedes this one.
