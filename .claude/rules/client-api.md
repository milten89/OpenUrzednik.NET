---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
  - "samples/**/*.cs"
---

# Client API ([ADR-0007](../../docs/adr/0007-client-api-and-extensibility.md))

- Parts that shape requests (the URL builders, `NbpTable`) are internal; tests replace them through an internal constructor. Each client has two public constructors: `(HttpClient)` and `(HttpClient, <Prefix>Options?, TimeProvider?, logger, traceSource)` (`NbpOptions`, `WhiteListOptions`). Gateways use the options' `ApiUrl` and a `DelegatingHandler`.
- Cross-cutting HTTP concerns belong in `DelegatingHandler`s. Auth that needs protocol calls (a session login, token acquisition) or credentials passed as API arguments belongs in the provider, designed in its provider ADR (ADR-0016).
