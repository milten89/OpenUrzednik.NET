---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# HTTP ([ADR-0019](../../docs/adr/0019-shared-http-layer-tested-through-test-host.md))

- A REST provider whose request paths or queries carry personal identifiers gives the executor a display path or route template ([ADR-0019](../../docs/adr/0019-shared-http-layer-tested-through-test-host.md)): `GetAsync(relativePath, displayPath, typeInfo, …)`; the display path replaces the URI in `url.path`, logs, error messages and `ErrorResponseContext.RequestPath`, and the default 400 mapping then leaves out the server's message (read and mask it in `MapErrorAsync` if needed).
- Its DI package calls `AddOpenUrzednikDisplayPathLogging()` on its named client, replacing `IHttpClientFactory`'s default loggers with ones that write the same entries with the display URI ([ADR-0017](../../docs/adr/0017-http-client-logging-for-identifier-paths.md)); its README says traces (`url.full`) still carry the path.
- A REST provider imports `src/Http/Http.props`, and its types stay `internal`: no public signature uses them.
- A provider describes itself with a `RestProviderProfile` (span name `<name>.http.get`, message prefix, optional `MapErrorAsync` override).
- NBP creates its executor in `Nbp/Common/NbpConnection.cs`.
- Options checks (the `https` base URL, `Timeout`, the base address from the options, the `HttpClient` or the default) are shared in `RestOptions` (`src/Http/Infrastructure`); a provider adds only its own rules.
- Clients take an `HttpClient` and optional `<Prefix>Options` (`NbpOptions`, `WhiteListOptions`); they never change the `HttpClient`.
- Dispose `HttpRequestMessage` and `HttpResponseMessage`.
