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
- A transport exception passes through unchanged ([#311](https://github.com/milten89/OpenUrzednik.NET/issues/311)): the `ConnectionFailedError` and `ServiceUnavailableError` messages of `NetworkFailure` append its `Message`, it is the error's `Exception` (the `InnerException` of the exception `EnsureSuccess()` throws), and it is logged and recorded on the span. The messages of .NET's own `HttpRequestException` and `IOException` name at most the host and port, not the path. A `DelegatingHandler` that an app adds is the app's: if it puts the request URI into its exception message, the raw identifier of a display-path request reaches those places. The library doesn't rewrite or mask exceptions it doesn't create; it masks only the text it composes (display paths, echoed identifiers).
