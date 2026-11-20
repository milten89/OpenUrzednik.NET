---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
  - "samples/**/*.cs"
---

# Client API ([ADR-0007](../../docs/adr/0007-client-api-and-extensibility.md))

- Parts that shape requests (the URL builders, `NbpTable`) are internal; tests replace them through an internal constructor. Each client has the public constructors `(HttpClient)` and `(HttpClient, <Prefix>Options? options = null, TimeProvider? timeProvider = null, OpenUrzednikTelemetry? telemetry = null)` (`NbpOptions`, `WhiteListOptions`; the telemetry container holds the logger, trace source and metrics, ADR-0025). Until 2.0 the NBP clients instead have prefix overloads without default values next to their obsolete 1.0 constructor. Gateways use the options' `ApiUrl` and a `DelegatingHandler`.
- Cross-cutting HTTP concerns belong in `DelegatingHandler`s. Auth that needs protocol calls (a session login, token acquisition) or credentials passed as API arguments belongs in the provider, designed in its provider ADR (ADR-0016).
