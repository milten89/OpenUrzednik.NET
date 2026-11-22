# OpenUrzednik.MF.WhiteList

🇵🇱 [Wersja polska (skrócona)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList/README.pl.md)

> ⚠️ **Unofficial:** OpenUrzednik.NET is a community project. It is not affiliated with, authorized or endorsed by the Polish Ministry of Finance, the National Revenue Administration (KAS) or any other Polish public institution.

**MF White List** client for OpenUrzednik.NET: the VAT taxpayers list (Wykaz podatników VAT, "Biała Lista") of the Ministry of Finance. Search the list by NIP, REGON or bank account number, check whether an account belongs to a NIP or REGON on a date, and read a NIP's VAT status from the WL2 service. The data comes from the [ministry's API](https://www.gov.pl/web/kas/api-wykazu-podatnikow-vat); the library redistributes none of it.

> Part of [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). See the [main README](https://github.com/milten89/OpenUrzednik.NET#readme) for an overview of the project.

## Installation

```bash
dotnet add package OpenUrzednik.MF.WhiteList
```

Requires [OpenUrzednik.Core](https://www.nuget.org/packages/OpenUrzednik.Core) and [OpenUrzednik.Transport.Http](https://www.nuget.org/packages/OpenUrzednik.Transport.Http) (NuGet installs them as dependencies).

## Usage

Two clients share the package: `WhiteListClient` for search and check (the API's `api/` path) and `WhiteListStatusClient` for the VAT status of a NIP (WL2, `api2/`). WL2 answers another question under another quota, so it is a separate client. Each is created in one line.

```csharp
using OpenUrzednik.MF.WhiteList;

using var httpClient = new HttpClient();
var client = new WhiteListClient(httpClient);
var date = new DateTime(2026, 11, 20);

// Search by NIP, with the proof of the check.
var search = await client.SearchByNipAsync("736-812-54-16", date);
if (search.IsSuccess)
{
    var subject = search.Value.Subject;
    Console.WriteLine($"{subject.Name}: {subject.VatStatus}");
    Console.WriteLine($"Proof: {search.Value.RequestId}, {search.Value.RequestDateTime:O}");
}
else if (search.Errors[0] is WhiteListNotFoundError notOnList)
    Console.WriteLine($"Not on the list. Proof: {notOnList.RequestId}, {notOnList.RequestDateTime:O}");
else
    Console.WriteLine(search.Errors[0].Message);

// Check whether an account belongs to a NIP on that date.
var check = await client.CheckNipBankAccountAsync("7368125416", "61 1090 1014 0000 0712 1981 2874", date);
if (check.IsSuccess)
    Console.WriteLine(check.Value.AccountAssigned ? $"On the list, proof {check.Value.RequestId}" : "Not assigned");

// The VAT status from WL2; without a date the API answers for its today.
var statusClient = new WhiteListStatusClient(httpClient);
var status = await statusClient.GetVatStatusByNipAsync("7368125416");
if (status.IsSuccess)
    Console.WriteLine($"{status.Value.VatStatus} on {status.Value.Date:yyyy-MM-dd}");
```

Pass your own host root (the test host, a proxy or an API gateway) and timeout in `WhiteListOptions`. The client doesn't change the `HttpClient`, so you can share it with other code:

```csharp
var client = new WhiteListClient(httpClient, new WhiteListOptions
{
    ApiUrl = WhiteListOptions.TestApiUrl,
    Timeout = TimeSpan.FromSeconds(5),
});
```

The client takes the host from the first place that sets one: `WhiteListOptions.ApiUrl`, `HttpClient.BaseAddress`, and finally the default `https://wykazpodatnikowvat.mf.gov.pl/`. It is the host root: each client adds `api/` or `api2/` itself, and a root whose path already ends with one of them is an `ArgumentException`. If you don't set `WhiteListOptions.Timeout`, the `HttpClient`'s timeout applies (100 s by default); `WhiteListOptions.Timeout` can shorten that limit but not extend it. The address must be an absolute `https` URL without a query (`?…`) or a fragment (`#…`): the constructor throws an `ArgumentException` for anything else.

With a DI container, use [OpenUrzednik.MF.WhiteList.DependencyInjection](https://www.nuget.org/packages/OpenUrzednik.MF.WhiteList.DependencyInjection): `builder.Services.AddOpenUrzednikMFWhiteList()` registers both clients.

Errors such as an exceeded daily limit, an entity that isn't on the list, a timeout or an invalid NIP come back in the result, not as exceptions. If you prefer exceptions, call `EnsureSuccess()` on the result. A `null` argument is a programming error: the method throws `ArgumentNullException` right away, before it returns a task. A resilience handler's rejections (timeout, open circuit breaker, rate limiter) become errors when the clients come from the DI package (`AddOpenUrzednikMFWhiteList()` or `AddKeyedOpenUrzednikMFWhiteList()`), or when you call `AddOpenUrzednikMFWhiteListRejectionHandler()` on your own client's builder; otherwise they come out as Polly exceptions.

## Methods

Every method takes a `CancellationToken` last. Identifiers may carry spaces and hyphens, and a NIP or account number may carry the `PL` prefix; the client sends the plain digits.

| Client | Method | Answers with |
|---|---|---|
| `WhiteListClient` | `SearchByNipAsync(nip, date)`, `SearchByRegonAsync(regon, date)` | `SubjectSearchResult`: the `Subject` and the proof |
| | `SearchByBankAccountAsync(bankAccount, date)` | `SubjectsSearchResult`: the `Subjects` that have the account, and the proof |
| | `SearchByNipsAsync(nips, date)`, `SearchByRegonsAsync(regons, date)`, `SearchByBankAccountsAsync(bankAccounts, date)` | `BatchSearchResult`: one `BatchSearchEntry` per identifier, and the proof; 1 to 30 identifiers |
| | `CheckNipBankAccountAsync(nip, bankAccount, date)`, `CheckRegonBankAccountAsync(regon, bankAccount, date)` | `AccountCheckResult`: `AccountAssigned` and the proof |
| `WhiteListStatusClient` | `GetVatStatusByNipAsync(nip)`, `GetVatStatusByNipAsync(nip, date)` | `VatStatusResult`: `VatStatus` (`Active`, `Exempt`, `NotRegistered`), its `Date` and the proof |

A `Subject` has the name, NIP, REGON, KRS, addresses, the VAT status, the registration, denial, restoration and removal dates with their legal bases, the account numbers, and, only for entities refused registration or struck off, the representatives, authorized clerks (`AuthorizedClerks`) and partners. The models don't expose PESEL: the ministry removed it from the list on 2021-01-01.

A batch call succeeds when the HTTP call does. Each entry keeps its identifier and either its subjects or its own error: an entry with no subject is a `WhiteListNotFoundError`, and an entry the API rejected is a `WhiteListQueryRejectedError`. The batch's proof is on the result. Duplicates are sent, as the API accepts them. Every invalid identifier of a batch is a `ValidationError<string>` named after its position (`nips[3]`) and the call returns all of them, so `EnsureSuccess()` throws them as one `OpenUrzednikValidationException`.

## API limits and quota

The ministry limits the API per IP address ([API page](https://www.gov.pl/web/kas/api-wykazu-podatnikow-vat)):

| What | Limit per day |
|---|---|
| Search (`search`) | 100 queries, each of up to 30 subjects |
| Check (`check`) | 5,000 entities |
| WL2 status | 50,000 queries |

The ministry says an IP that goes over the limit, or queries too fast, "may" be blocked until midnight, for the API and for its web search alike. The blocking is discretionary and its rules aren't published. It doesn't document whether a batch counts as one of the 100 searches or as up to 30, and when we probed the hosts on 2026-11-08 the limit wasn't enforced. Plan for the stated limits: the exhausted daily limit is an HTTP 429 with the code `WL-191`.

- `RetryAfter` is an estimate. On a 429 the error is a `WhiteListRateLimitExceededError` (a `RateLimitExceededError`) and its `RetryAfter` is the server's `Retry-After` when the response has one. The API documents none, so it is usually the time until the next midnight in Europe/Warsaw. That estimate comes from the ministry's statement above, not from a contract. An app that needs certainty reads `WhiteListCode` (`WL-191`) and decides for itself. A retry before `RetryAfter` spends quota and may extend the block.
- `WhiteListCode`: every provider error type (`WhiteList…Error`) except `WhiteListNotFoundError` implements `IWhiteListErrorCode` with `string? WhiteListCode`: the API's code (`WL-` and three digits, e.g. `WL-191`, or a WL2 letter: `I`, `D`, `X`), or `null` when the answer had none or had another value. The code is also in the error's `Metadata` under `whiteListCode` (`WhiteListErrorMetadata.WhiteListCodeMetadataKey`).
- Updates: the list is updated on working days, and the API may be disturbed between 22:00 and 24:00: `WL-195` ("the database was updated, repeat the query") and `WL-196` ("update in progress, try later") are worth a retry, see [Resilience](#resilience).
- Local validation saves quota: the client rejects what the API would reject before it sends anything: an invalid identifier, a date outside the range below, an empty batch or one over 30.

The ministry points bulk checks to the [flat file](https://www.gov.pl/web/kas/wykaz-podatnikow-vat) of the list. Art. 96b ust. 2 of the VAT act requires the list to be available for checking "w tym automatycznie" (including automatically).

## Dates

Every `api/` method takes a required `date`, because the date is part of what the proof proves. On WL2 it is optional: `GetVatStatusByNipAsync(nip)` lets the API answer for its today, and the result's `Date` says which day that was.

- Only the date part counts: the client ignores the time of day and `DateTime.Kind`.
- The date must not be in the future and not before 1 January of the fifth year back, both by the Warsaw calendar (art. 96b ust. 2 of the VAT act). Another date is a `ValidationError<DateTime>` and no request is sent.
- The ministry doesn't document WL2's range. The test host enforced the same one on 2026-11-21 (a date outside it answered `D`, "date out of range"), which is also a `WhiteListQueryRejectedError` if the ministry changes it later.

On .NET 8 and later every date method also has a `DateOnly` overload (extension methods in `OpenUrzednik.MF.WhiteList`); `date` in models and arguments is a `DateTime` on every target, and the package also runs on .NET Framework 4.7.2 or later through `netstandard2.0` with the same API.

## The proof of a check (`requestId`)

Every answer of the register carries `RequestId` and `RequestDateTime`, on the result or, when a searched subject isn't on the list, on the `WhiteListNotFoundError` (`RequestId`, `RequestDateTime`). `RequestDateTime` is a `DateTimeOffset` with the Warsaw offset of the API's local time. The text has no offset, so the hour repeated when daylight saving time ends gets the standard offset (`+01:00`).

The ministry stores every query, and the `requestId` "may confirm" before the tax authorities that the query was made. The ministry advises keeping it. Three rules in Polish law make the check matter:

- When a VAT taxpayer pays for a transaction between businesses worth more than 15,000 PLN, however many payments it is split into ([art. 19 Prawo przedsiębiorców](https://api.sejm.gov.pl/eli/acts/DU/2025/1480/text.pdf)), to an account that isn't on the list on the day the transfer is ordered, the payer becomes jointly liable for the supplier's VAT arrears in the part that falls to that supply ([art. 117ba Ordynacja podatkowa](https://api.sejm.gov.pl/eli/acts/DU/2026/622/text.pdf); with exceptions, among them split payment and a notification to the tax office within 7 days).
- The payment isn't a tax cost to that extent ([art. 15d CIT](https://api.sejm.gov.pl/eli/acts/DU/2026/554/text.pdf), [art. 22p PIT](https://api.sejm.gov.pl/eli/acts/DU/2026/592/text.pdf)).
- The list itself is kept under [art. 96b of the VAT act](https://api.sejm.gov.pl/eli/acts/DU/2026/1263/text.pdf).

The library returns the data and the proof as the API gives them. It gives no legal advice: ask your tax adviser how these rules apply to you.

## The host changed

The ministry moved the API to `https://wykazpodatnikowvat.mf.gov.pl/` (test: `https://wykazpodatnikowvat-test.mf.gov.pl/`). The old hosts `wl-api.mf.gov.pl` and `wl-test.mf.gov.pl` work only until 2026-12-01, and the library doesn't support them. `WhiteListOptions.DefaultApiUrl` and `WhiteListOptions.TestApiUrl` hold the current hosts. The test host answers with the ministry's [test data](https://www.gov.pl/web/kas/api-wykazu-podatnikow-vat): lists of NIPs, REGONs and account numbers that return fictitious subjects. Use it to test an integration without querying the real register.

## Errors

The API's own errors come back as the provider's error types. They derive from the library's condition errors, so `is NotFoundError`, `is BadRequestError` and the others keep working; see [Errors](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Core/README.md#errors) for the base types.

| What happened | Error |
|---|---|
| The IP exceeded the daily limit (HTTP 429) | `WhiteListRateLimitExceededError` (a `RateLimitExceededError`, with `RetryAfter` and the HTTP status) |
| The list is being updated: HTTP 400 `WL-195` or `WL-196` | `WhiteListServiceUnavailableError` (a `ServiceUnavailableError`) |
| Another 5xx (including `WL-100`) | `WhiteListServiceUnavailableError` |
| Any other 400, including `WL-101` to `WL-118`, `WL-130` and `WL-190`: the API rejected a parameter | `WhiteListBadRequestError` (a `BadRequestError`, with the HTTP status) |
| HTTP 500 `WL-197`: the system can't determine the historical status | `WhiteListUnknownError` (an `UnknownError`; a retry can't fix it and would spend quota) |
| A rejection inside a successful answer: a batch entry `error`, or WL2 `I` (invalid NIP) or `D` (date out of range) | `WhiteListQueryRejectedError` (a `BadRequestError`) |
| WL2 `X`: the service is inactive | `WhiteListServiceInactiveError` (a `ServiceUnavailableError`) |
| The entity isn't on the list (a search with no subject, an account with no subject, WL2 `B`) | `WhiteListNotFoundError` (a `NotFoundError`, with the proof) |
| Any other status (401, 403, 404, 408, …) | The shared HTTP error for that status, e.g. `HttpNotFoundError`, with the code in the message |
| A bad argument | `ValidationError<string>` for an identifier (the value is masked), `ValidationError<DateTime>` for a date, `ValidationError<int>` for a batch that is empty or has more than 30 identifiers |
| Timeout, no connection, a body that isn't the expected JSON | `RequestTimeoutError`, `ConnectionFailedError`, `SerializationError` |

An entity that isn't on the list is an error, not an empty success, so `EnsureSuccess()` stops on it and `is NotFoundError` handles it, but the proof isn't lost: it is on the error. A check never ends in `WhiteListNotFoundError`: an answer has `AccountAssigned` `true` or `false` (validation, HTTP and serialization failures still apply). Messages and metadata hold the API's code only in its documented shape, never an identifier: identifiers an API message echoes are masked, also with separators (`736-812-54-16`).

## Personal data

The requests carry NIP, REGON and account numbers in the URL path, and responses carry a sole trader's name, addresses, NIP and accounts, and, for entities refused registration or struck off, the names and NIPs of representatives, proxies and partners ([art. 96b ust. 3–4 VAT act](https://api.sejm.gov.pl/eli/acts/DU/2026/1263/text.pdf)). The library never logs or puts that response data into errors: it goes only to you. What you do with it (storing, sharing, retention) is your responsibility under the GDPR. NIP, REGON and account numbers never appear unmasked in the library's own logs, spans, metrics, error messages or metadata (masking keeps the last four characters: `******5416`).

## HttpClient and thread safety

The clients don't own the `HttpClient`, never change it and aren't `IDisposable`. Create one `HttpClient` for the application, or take it from `IHttpClientFactory` (the [DependencyInjection package](https://www.nuget.org/packages/OpenUrzednik.MF.WhiteList.DependencyInjection) does that), and share it. Don't create an `HttpClient` per request: it exhausts sockets. The clients are thread-safe: create one client per `HttpClient` and share it across threads and concurrent calls.

## Compatibility

Minor versions may add methods to the client interfaces (`IWhiteListClient`, `IWhiteListStatusClient`), properties to the models and options to `WhiteListOptions` ([ADR-0011](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0011-compatibility-policy.md)). The clients and `WhiteListOptions` are `sealed`: to swap or wrap a client, use its interface. If you implement an interface yourself, a new method breaks your build until you add it. New model properties are `init` properties.

The .NET builds (`net8.0`, `net10.0`) are trimming- and Native AOT-compatible: publishing an app with trimming or Native AOT gives no warning from this package. CI checks it on every change ([ADR-0014](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0014-trimming-and-native-aot.md)). The `netstandard2.0` build (.NET Framework) isn't covered.

## Resilience

The package adds no retries or timeouts, and neither does the DI package: the app owns its resilience handler. A handler decides from the HTTP status only and can't see the API's codes, so the package adds `WhiteListRetry.ShouldRetryAsync(HttpResponseMessage, CancellationToken)` (a `ValueTask<bool>`, the type of Polly's `ShouldHandle`). It answers yes for 400 `WL-195` and `WL-196`, 408 and 5xx, and no for 429 (the IP may be blocked until midnight), 500 `WL-197` and anything else. It reads at most 4,096 bytes (plus one, to tell a larger body apart) of a 400 or 500 body to find the code and puts them back, so the client's error mapping still reads the whole body. It uses only BCL types, so it fits any resilience library.

The [DI package README](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.md#resilience) shows how to plug it into `AddStandardResilienceHandler()`, and the .NET Framework variant.

## Logging and tracing

The clients' last constructor parameter, `telemetry`, takes an `OpenUrzednikTelemetry` with `Logger`, `TraceSource` and `Metrics` properties (pass `null` for `options` and `timeProvider` to keep their defaults); a property you leave `null` keeps its default. Without a logger they log nothing. Without a trace source, on .NET 8 and later they record spans on the `OpenUrzednik.MF.WhiteList` `ActivitySource` (`WhiteListTelemetry.SourceName`), which costs nothing while no one listens to it; on .NET Framework they record none. To use `ILogger` and OpenTelemetry, install the [OpenUrzednik.Extensions.Logging](https://www.nuget.org/packages/OpenUrzednik.Extensions.Logging) and [OpenUrzednik.Diagnostics](https://www.nuget.org/packages/OpenUrzednik.Diagnostics) adapters:

```csharp
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.MF.WhiteList;

var client = new WhiteListClient(httpClient, options: null, timeProvider: null, telemetry: new OpenUrzednikTelemetry
{
    Logger = loggerFactory.CreateOpenUrzednikLogger<WhiteListClient>(),
    TraceSource = ActivityTraceSource.GetShared(WhiteListTelemetry.SourceName),
    Metrics = MeterMetrics.GetShared(WhiteListTelemetry.SourceName),
});
```

On .NET 8 and later the `TraceSource` and `Metrics` lines are optional; to turn spans off, set `TraceSource = NullOpenUrzednikTraceSource.Instance`. Subscribe with `AddSource("OpenUrzednik.*")` or `AddSource(WhiteListTelemetry.SourceName)`. Logs go to a category named after the client's full type name (e.g. `OpenUrzednik.MF.WhiteList.WhiteListClient`).

Without `Metrics`, on .NET 8 and later the clients record each operation's duration on the `OpenUrzednik.MF.WhiteList` `Meter`, which costs nothing while no one listens to it; on .NET Framework they record none. Set `Metrics = MeterMetrics.GetShared(WhiteListTelemetry.SourceName)` (OpenUrzednik.Diagnostics) to record on .NET Framework too (the DI package passes metrics for you, [on its container's meter factory when it has one](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Diagnostics/README.md#with-dependency-injection)), and subscribe with `AddMeter("OpenUrzednik.*")` or `AddMeter(WhiteListTelemetry.SourceName)`.

The clients use their own interfaces (`IOpenUrzednikLogger`, `IOpenUrzednikTraceSource` and `IOpenUrzednikMetrics` from OpenUrzednik.Core), not `ILogger`, `ActivitySource` and `Meter`. Only the adapters connect them.

Traces still carry the path. Requests use a display path (`search/nip/{nip}`) in `url.path`, in logs, in messages and in the error's request path, so the library's own telemetry has no identifier. .NET's `HttpClient` instrumentation (OpenTelemetry's `AddHttpClientInstrumentation()`, the `System.Net.Http` activity source) records the real request URI, so `url.full` in those traces carries the NIP, REGON or account number in the path ([ADR-0019](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0019-shared-http-layer-tested-through-test-host.md)). Filter or redact `url.full` in your exporter, or don't enable that instrumentation for this client. With the DI package, `IHttpClientFactory`'s own log entries show the display path ([ADR-0017](https://github.com/milten89/OpenUrzednik.NET/blob/main/docs/adr/0017-http-client-logging-for-identifier-paths.md)).

## Telemetry reference

Within a major version, the names below don't change: the source and meter, span names, the metric's name, unit and tags, `error.type` values, logger categories and event ids. Minor versions may add spans, metrics, tags and events. Log message text and properties may change in any version, so filter logs by event id, not by text.

### Spans

Each client method starts one span on the `OpenUrzednik.MF.WhiteList` source, with the HTTP request as its child `mf_white_list.http.get`. All spans are of kind `Internal`.

| Span | Client and method | Tags |
|---|---|---|
| `mf_white_list.search.nip` | `WhiteListClient.SearchByNipAsync` | `mf_white_list.nip`, `mf_white_list.date` |
| `mf_white_list.search.regon` | `SearchByRegonAsync` | `mf_white_list.regon`, `mf_white_list.date` |
| `mf_white_list.search.bank_account` | `SearchByBankAccountAsync` | `mf_white_list.bank_account`, `mf_white_list.date` |
| `mf_white_list.search.nips`, `.search.regons`, `.search.bank_accounts` | `SearchByNipsAsync`, `SearchByRegonsAsync`, `SearchByBankAccountsAsync` | `mf_white_list.count`, `mf_white_list.date` |
| `mf_white_list.check.nip_bank_account` | `CheckNipBankAccountAsync` | `mf_white_list.nip`, `mf_white_list.bank_account`, `mf_white_list.date` |
| `mf_white_list.check.regon_bank_account` | `CheckRegonBankAccountAsync` | `mf_white_list.regon`, `mf_white_list.bank_account`, `mf_white_list.date` |
| `mf_white_list.status.nip` | `WhiteListStatusClient.GetVatStatusByNipAsync` | `mf_white_list.nip`, `mf_white_list.date` (only the overload with a date) |
| `mf_white_list.http.get` | the request of any method | `http.request.method`, `server.address` (e.g. `wykazpodatnikowvat.mf.gov.pl`, or the `ApiUrl` host), `server.port` (always set, e.g. `443`), `url.path` (the display path, e.g. `/api/search/nip/{nip}`), `http.response.status_code` |

Tag values: `mf_white_list.nip`, `mf_white_list.regon` and `mf_white_list.bank_account` are always masked (only the last four characters stay visible), `mf_white_list.date` is the date as `yyyy-MM-dd`, and `mf_white_list.count` is the number of identifiers of a batch, which carries none of them. A failed span has the `Error` status and an `error.type` tag with the error code (see [Errors](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Core/README.md#errors)). A method that throws, a cancelled call included, gets `error.type` `_OTHER`, and so does its `mf_white_list.http.get` span. A batch call whose entries failed is a success, so its span has no `error.type`.

### Metrics

Each client method records one measurement on the `OpenUrzednik.MF.WhiteList` meter when it ends, while its span is still current (so exemplars can link to the trace).

| Instrument | Kind, unit | Tags |
|---|---|---|
| `openurzednik.client.operation.duration` | histogram, seconds (`s`) | `openurzednik.operation` (the method's span name above, e.g. `mf_white_list.search.nip`), `openurzednik.provider` (`mf_white_list`), `error.type` (only on failure), `server.address`, `server.port` |

The duration is exactly the span's when the operation is traced, and otherwise measured with the client's `TimeProvider`. `error.type` is the first error's code (as on the span), or `_OTHER` when the method threw, e.g. because the caller cancelled it. `notFound` is a normal answer, not an outage: a subject that isn't on the list comes back as a `WhiteListNotFoundError` with its proof, so leave it out of error-rate alerts (`error.type != notFound`). A 404 from the server (a wrong `ApiUrl`, a gateway, a moved host) is `notFound` too, as an `HttpNotFoundError`: watch for it through the `mf_white_list.http.get` span's `http.response.status_code` or log event 1101 (`NotFound`), which a register answer never produces. The suggested bucket boundaries (OpenTelemetry's for RPC calls, 0.1 ms to 60 s) apply on .NET 10 and .NET Framework; on .NET 8 the listener's defaults apply. No tag carries an identifier.

### Log events

Every entry has an event id; `WhiteListLogEvents` lists them as constants, and the event name is the constant's name (`EventId(1001, "RequestTimedOut")`). The ids are the same in every client. The `{provider}` property is `mf_white_list`. No entry carries an identifier unmasked, and none carries response data.

| Id | Name | Level | When |
|---|---|---|---|
| 1001 | `RequestTimedOut` | Warning | A request timed out (`WhiteListOptions.Timeout`, `HttpClient.Timeout` or a resilience handler's timeout). |
| 1002 | `ConnectionFailed` | Warning | The API couldn't be reached (DNS, TLS, connection) or the body read broke. |
| 1003 | `RequestRejected` | Warning | A resilience handler rejected the request (open circuit breaker, rate limiter). |
| 1101 | `NotFound` | Debug | The API returned 404. |
| 1102 | `BadRequest` | Warning | The API returned 400. Not written today: `ErrorResponse` covers every 400. |
| 1103 | `Unauthorized` | Warning | The API returned 401 or 403. |
| 1104 | `RequestTimeoutStatus` | Warning | The API returned 408. |
| 1105 | `RateLimited` | Warning | The API returned 429. Not written today: `ErrorResponse` covers every 429. |
| 1106 | `ServiceUnavailable` | Warning | The API returned a 5xx status. Not written today: `ErrorResponse` covers every 5xx. |
| 1107 | `UnexpectedStatus` | Warning | The API returned another non-success status. |
| 1108 | `ErrorResponse` | Warning | The error mapping turned a 400, 429 or 5xx response into one of the provider's errors, with the API's code when it has the documented shape. |
| 1109 | `ErrorBodyUnreadable` | Debug | The body of an error response couldn't be read. |
| 1201 | `EmptyResponse` | Error | A success response had no payload. |
| 1202 | `DeserializationFailed` | Error | A success response wasn't the expected JSON. |
| 1203 | `DecodingFailed` | Error | A success response declared a charset that can't be decoded, or its compressed body couldn't be decompressed. |
| 2001 | `ValidationFailed` | Debug | The arguments failed validation; no request was sent. |
| 2002 | `NotOnList` | Debug | The register has no entity for the identifier: a search with no subject, an account with no subjects, or WL2 `B`; the answer is a `WhiteListNotFoundError` with the proof. A batch with failed entries also writes one entry, with the counts of entries not on the list and of rejected entries. |
| 2003 | `MappingFailed` | Error | A response lacked data the model needs. |
| 2004 | `QueryRejected` | Warning | WL2 answered `I`, `D` or `X` (invalid NIP, date out of range, service switched off). Only the operation and the one-letter code are logged. Batch entry errors are counted under 2002. |
