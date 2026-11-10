---
status: accepted
date: 2026-11-08
confidence: medium
decision-makers: milten89
---

# 0023. MF White List provider (`OpenUrzednik.MF.WhiteList`)

## Context and Problem Statement

[ADR-0016](0016-next-providers-and-package-naming.md) chose the Ministry of Finance VAT White List HTTP API (Wykaz podatników VAT) as one of the next providers, named its package `OpenUrzednik.MF.WhiteList` (short name `mf_white_list`), and asked its provider ADR to answer a readiness checklist and choose a type prefix. Businesses query the list before they pay an invoice: paying for a large transaction to an account not on the list has tax consequences for the payer, and the API's `requestId` is the proof that the check was made. The API takes Polish identifiers in its URL paths, returns personal data of sole traders, has a daily per-IP quota that may block the caller until midnight, answers batch queries with per-entry errors inside a success, and returns its own error codes. How does `OpenUrzednik.MF.WhiteList` expose this API within the library's contracts?

## Decision Drivers

* The library's contracts: results and the condition errors (ADR-0002, [ADR-0021](0021-transport-subclasses-of-condition-errors.md)), telemetry and masking (ADR-0003), the shared HTTP layer with display paths ([ADR-0019](0019-shared-http-layer-tested-through-test-host.md), [ADR-0017](0017-http-client-logging-for-identifier-paths.md)), the client API (ADR-0007), public dates (ADR-0005), dependencies (ADR-0004), Native AOT (ADR-0014).
* The proof of a check (`requestId`, `requestDateTime`) must reach the caller for every answer of the register, also when the entity isn't on the list.
* Identifiers (NIP, REGON, account numbers) never appear unmasked in telemetry, messages or metadata; personal data in responses go to the caller and nowhere else.
* The quota is small (100 searches a day): the client rejects what the API would reject before it sends anything.
* A later flat-file package ([#220](https://github.com/milten89/OpenUrzednik.NET/issues/220)) reuses the models that don't depend on the HTTP API.

## Considered Options

* One client for the `/api` search and check plus one client for WL2 status, the API's own error codes as provider error types, the proof in API-specific results, and local validation of everything the API documents
* One client for all endpoints
* An entity that isn't on the list as a success with an empty subject
* `requestDateTime` as a `DateTime` with `DateTimeKind.Unspecified`
* A retry extension on `HttpStandardResilienceOptions` in the DI package
* Validation by the server only
* WL2's error codes (I, D, X) and "not in the register" (B) as values of the status enum

## Decision Outcome

Chosen option: the first, because it follows the reference provider's structure, keeps each answer's proof with its data, gives apps the API's codes as typed details, and spends no quota on requests the API would reject.

**Readiness checklist (ADR-0016)**

* **Protocol and methods:** REST/JSON over HTTPS, `GET` only, through the shared HTTP layer. Production `https://wykazpodatnikowvat.mf.gov.pl/` with `api/` (search and check) and `api2/` (WL2 status); test `https://wykazpodatnikowvat-test.mf.gov.pl/`. The old hosts `wl-api.mf.gov.pl` and `wl-test.mf.gov.pl` work only until 2026-12-01 and aren't supported.
* **Authentication and sessions:** none.
* **Quota and retry advice:** per IP: `search` 100 queries a day of up to 30 subjects, `check` 5,000 entities, WL2 50,000 queries a day. The ministry says an IP over the limit, or one that queries too fast, "may" be blocked until midnight, for the API and its web search alike; the blocking is discretionary and its rules aren't published. The error-code table gives `WL-191` with HTTP 429 for an exhausted daily limit and documents no `Retry-After`. On 2026-11-08 the limit wasn't enforced (the test host answered 130 single searches; production answered 160 single searches and 120 batches of 30 from one IP, all with 200), so the blocked response is known only from the table. The list is updated on working days, with possible disruption between 22:00 and 24:00 (`WL-195` "the database was updated, repeat the query", `WL-196` "update in progress, try later"). Retry advice: below, **Errors** and **Resilience**.
* **Personal data:** requests carry NIP, REGON and account numbers in the URL path. Responses carry a sole trader's name, addresses, NIP and accounts, and, only for entities refused registration or struck off, the names and NIPs of representatives, proxies (`authorizedClerks`) and partners ([art. 96b ust. 3–4 VAT act](https://api.sejm.gov.pl/eli/acts/DU/2026/1263/text.pdf)). PESEL was removed from the list on 2021-01-01 ([Dz.U. 2020 poz. 2419](https://api.sejm.gov.pl/eli/acts/DU/2020/2419/text.pdf)), though the contract still declares nullable `pesel` fields: the models don't expose them. Masking: below, **Telemetry and privacy**.
* **Fixtures and test environment:** a test host exists, with the ministry's test data (lists of NIPs, REGONs and account numbers that return fictitious subjects). Fixtures are captured from the test host, or from production for a legal person (e.g. the Ministry of Finance); a remaining personal field gets a fake value of the same format. The 429 / `WL-191` body is built from the error-code table and marked as such until a real one is captured. The `/verify-api` notes list the test data and the probes.
* **Terms of use and attribution:** the API page publishes no terms, no licence for the data and no attribution requirement; there is no authentication or registration. The library redistributes no data; the READMEs name the source (Ministry of Finance, KAS).
* **Legal basis for automated access:** art. 96b ust. 2 of the VAT act requires the list to be available for checking "w tym automatycznie" (including automatically); the ministry publishes the API for external systems and points bulk checks to the flat file.
* **Legal meaning of outputs:** the list is kept under art. 96b of the VAT act. When a VAT taxpayer pays for a transaction between businesses worth more than 15,000 PLN, however many payments it is split into ([art. 19 Prawo przedsiębiorców](https://api.sejm.gov.pl/eli/acts/DU/2025/1480/text.pdf)), to an account not on the list on the day the transfer is ordered, the payer becomes jointly liable for the supplier's VAT arrears in the part that falls to that supply ([art. 117ba Ordynacja podatkowa](https://api.sejm.gov.pl/eli/acts/DU/2026/622/text.pdf); with exceptions, among them split payment and a notification to the tax office within 7 days), and the payment isn't a tax cost to that extent ([art. 15d CIT](https://api.sejm.gov.pl/eli/acts/DU/2026/554/text.pdf), [art. 22p PIT](https://api.sejm.gov.pl/eli/acts/DU/2026/592/text.pdf)). The ministry stores every query; `requestId` "may confirm" before the tax authorities that the query was made, and the ministry advises keeping it. The library returns the data and the proof as the API gives them and gives no legal advice; the README links the acts.
* **Partial results of batch calls:** below, **Batch search**.
* **Extra dependencies:** none. The retry filter (below, **Resilience**) uses only BCL types, so neither package references a resilience library beyond what ADR-0004 already allows. The flat file needs a 7z reader and stays a separate package with its own ADR (#220).
* **Package and namespace:** `OpenUrzednik.MF.WhiteList` and `OpenUrzednik.MF.WhiteList.DependencyInjection` (ADR-0016); models in area namespaces `OpenUrzednik.MF.WhiteList.Search`, `.Check` and `.Status`.

**Names**

* Type prefix `WhiteList` (the namespace already says MF): `WhiteListOptions`, `WhiteListTelemetry`, `WhiteListLogEvents`, the clients and the error types below. The DI methods carry the package name: `AddOpenUrzednikMFWhiteList()`, `AddKeyedOpenUrzednikMFWhiteList()`, `AddOpenUrzednikMFWhiteListRejectionHandler()`.
* `ActivitySource` `OpenUrzednik.MF.WhiteList`; short name `mf_white_list`.

**Clients (ADR-0007)**

* `WhiteListClient` (`IWhiteListClient`): search and check on `api/`, in `partial` files by area (`.Nip`, `.Regon`, `.BankAccount`, `.Check`). `WhiteListStatusClient` (`IWhiteListStatusClient`): WL2 on `api2/`, which answers another question (only the VAT status) under another quota. Each has the two constructors of ADR-0007.
* One method per operation: a search by one NIP, REGON or account; a batch search by NIPs, REGONs or accounts (separate methods taking up to 30 identifiers); a check of an account against a NIP or a REGON; the WL2 status of a NIP.
* `date` is a required `DateTime` on every `api/` method, as in the API, because it is part of what the proof proves; on WL2 it is optional, as in the API. Converted with `DateOnly.FromDateTime` (ADR-0005).
* `WhiteListOptions.ApiUrl` is the host root (default production); each client's base address adds `api/` or `api2/`, and its paths are relative to that. A root whose path already ends with `api/` or `api2/` (in any case), from the options or from `HttpClient.BaseAddress`, is an `ArgumentException` that asks for the host root. A documented constant holds the test host.

**Validation before sending**

* `null` throws `ArgumentNullException` synchronously (ADR-0002).
* NIP, REGON and account numbers go through the shared validators of `src/Identifiers` (separators, and a `PL` prefix for NIP and accounts, are accepted; the normalized digits are sent); an invalid one is a `ValidationError<string>` with the value masked.
* A date, required on `api/` and optional on WL2, must not be in the future and not before 1 January of the fifth year back, both by the Warsaw calendar (`TimeProvider`), as art. 96b ust. 2 sets for the list: a `ValidationError<DateTime>`. The ministry doesn't document WL2's range; its `D` code (below) covers a difference.
* A batch takes 1 to 30 identifiers: a `null` collection or a `null` element throws `ArgumentNullException`, and an empty one or more than 30 is a `ValidationError`. Every invalid identifier is a `ValidationError<string>` named after its position (`nips[3]`, the value masked), and the call returns all of them, which `EnsureSuccess()` throws as one `OpenUrzednikValidationException` (ADR-0002). Duplicates are sent, as the API accepts them.

**Answers and the proof**

* Every answer of the register carries its proof: `RequestId` (`string`) and `RequestDateTime`, a `DateTimeOffset` with the Warsaw offset of the API's `dd-MM-yyyy HH:mm:ss` local time. The text has no offset, so the hour repeated when daylight saving time ends gets the standard offset (`+01:00`); ADR-0005's `DateTime` rule covers dates, and this is an instant. In `Metadata` it is an ISO 8601 string with the offset (ADR-0021).
* The proof is in API-specific result records next to transport-neutral models (a subject, its persons and accounts, the VAT status enum `Active`, `Exempt`, `NotRegistered`), so the flat-file package can reuse the models without it. Public dates in models are `DateTime` (ADR-0005).
* An entity that isn't on the list is `WhiteListNotFoundError : NotFoundError` with `RequestId` and `RequestDateTime` (metadata keys `requestId` and `requestDateTime`), so `is NotFoundError` handles it and the proof isn't lost: on `api/` a search with no subject (`subject: null`, or `subjects: []` for an account), and on WL2 the status `B` ("not in the VAT register"), the same case.
* The check's `accountAssigned` (`TAK`/`NIE`, about an active entity) is a `bool`; any other value is a `SerializationError`.
* WL2's other `statusCode` values `C`, `Z` and `N` are the status (active, exempt, not registered as a VAT taxpayer). `I` (invalid NIP) and `D` (date out of range) are a `WhiteListQueryRejectedError` and `X` ("service inactive") a `WhiteListServiceInactiveError` (below), never statuses, so code that switches on the status never takes a failed query for an answer.

**Batch search**

* The call succeeds when the HTTP call succeeds. Each entry keeps its identifier and either its subjects or an `OpenUrzednikError`: an entry with no subject is a `WhiteListNotFoundError` with the batch's proof, and an entry `error` (`{code, message}`) a `WhiteListQueryRejectedError`. The batch's proof is on the result.

**Errors**

The profile's `MapErrorAsync` reads `{message, code}`, masks any identifier the message echoes, and returns the provider subclasses of [ADR-0021](0021-transport-subclasses-of-condition-errors.md) ("Provider details"). The error types and `IWhiteListErrorCode` are in the root namespace `OpenUrzednik.MF.WhiteList`. Every provider error type except `WhiteListNotFoundError` implements `IWhiteListErrorCode` with `string? WhiteListCode`, the API's code when it has the documented shape (`WL-` and three digits, e.g. `WL-191`, or one upper-case WL2 letter, `I`, `D`, `X`), `null` when the answer had none or had another value, which is never echoed into messages or metadata (ADR-0021's example shows the property without `?`; it is nullable because a 429 or 400 may come without a body); metadata key `whiteListCode`, the constant `WhiteListErrorMetadata.WhiteListCodeMetadataKey`, since six types share it. `WhiteListNotFoundError`'s own keys `requestId` and `requestDateTime` are constants on it (`RequestIdMetadataKey`, `RequestDateTimeMetadataKey`). A rejected query has two types by where the rejection came from: `WhiteListBadRequestError` for an HTTP 400 (with its status) and `WhiteListQueryRejectedError` for a rejection inside a 200; `is BadRequestError` covers both.

| Answer | Error |
|---|---|
| 429 | `WhiteListRateLimitExceededError : HttpRateLimitExceededError` |
| 400 `WL-195`, `WL-196` | `WhiteListServiceUnavailableError : HttpServiceUnavailableError` |
| 400 with another code (incl. `WL-101`–`WL-118`, `WL-130`, `WL-190`) or none | `WhiteListBadRequestError : HttpBadRequestError` (a server-rejected parameter is a bad request, ADR-0021) |
| 500 `WL-197` ("the system can't determine the historical status") | `WhiteListUnknownError : HttpUnknownError` |
| another 5xx (incl. `WL-100`) | `WhiteListServiceUnavailableError` |
| any other status (401, 403, 404, 408, …), none documented | the shared layer's default `Http…Error`, the code in its message |
| 200, a batch entry `error`, or WL2 `I` or `D` | `WhiteListQueryRejectedError : BadRequestError` |
| 200, WL2 `X` | `WhiteListServiceInactiveError : ServiceUnavailableError` |
| 200, not on the list (`api/` no subject, WL2 `B`) | `WhiteListNotFoundError : NotFoundError` |

**When the daily limit is hit**, `RetryAfter` is the server's `Retry-After` when present. Otherwise it is the time until the next midnight in Warsaw. This is an estimate, not a contract: it rests on the ministry's statement that an over-limit IP "may be blocked until midnight" (*"może być zablokowany do godziny 0:00"*). That blocking is discretionary, its rules for querying too fast aren't published, and it wasn't observed on 2026-11-08. The XML docs and the README say so. An app that needs certainty reads `WhiteListCode` (`WL-191`) and decides itself.

**Resilience**

* As ADR-0004 says, the DI package adds no resilience handler, and the app owns its handler (retries, delays, timeouts, circuit breaker). A resilience handler decides from the HTTP status only and can't see the API's codes, so the base package adds a retry filter for the app's handler: a public `WhiteListRetry.ShouldRetryAsync(HttpResponseMessage, CancellationToken)` that answers whether a response is worth retrying. Yes for 400 `WL-195`/`WL-196` and for 408 and 5xx; no for 429 (blocked until midnight), 500 `WL-197` (a retry can't fix it and would spend quota) and anything else. It uses only BCL types, so it fits any resilience library; it buffers the body before reading the code, so the error mapping can still read it.
* The README shows how an app plugs it into `AddStandardResilienceHandler()`'s `options.Retry.ShouldHandle`, next to `HttpClientResiliencePredicates.IsTransient` for exceptions and timeouts. Neither package references `Microsoft.Extensions.Http.Resilience` or exposes Polly types (ADR-0004).

**Telemetry and privacy**

* Spans `mf_white_list.search.nip`, `.search.nips`, `.search.regon`, `.search.regons`, `.search.bank_account`, `.search.bank_accounts`, `.check.nip_bank_account`, `.check.regon_bank_account`, `.status.nip`; the HTTP child `mf_white_list.http.get`.
* Tags: `mf_white_list.date`; `mf_white_list.nip`, `mf_white_list.regon` and `mf_white_list.bank_account`, always through `SensitiveValue.Mask`; `mf_white_list.count` for a batch, which carries no identifiers.
* Every request uses a display path (`search/nip/{nip}`, `search/nips/{nips}`, `check/nip/{nip}/bank-account/{account}`, `status/nip/{nip}`, relative to the client's base address), which replaces the URI in `url.path`, logs, messages and `ErrorResponseContext.RequestPath` ([ADR-0019](0019-shared-http-layer-tested-through-test-host.md)); the DI package uses `AddOpenUrzednikDisplayPathLogging()` ([ADR-0017](0017-http-client-logging-for-identifier-paths.md)). The README says that traces (`url.full`) still carry the path.
* Response data (names, addresses, accounts) is never logged or put into errors; it goes only to the caller, who is responsible for it under the GDPR.
* `WhiteListLogEvents` lists the shared HTTP events (1xxx) and the provider's own (2xxx).

**Implementation:** issues [#257](https://github.com/milten89/OpenUrzednik.NET/issues/257)–[#264](https://github.com/milten89/OpenUrzednik.NET/issues/264), one per PR. The packages stay `IsPackable=false` until the last one (ADR-0016).

### Consequences

* Good, because every answer of the register reaches the caller with its proof, including "not on the list", the same way on `api/` and WL2.
* Good, because apps act on the API's codes through one interface (`IWhiteListErrorCode`) on every error a code reaches, and keep the HTTP status where there is one.
* Good, because local validation saves quota, reports every invalid identifier of a batch at once, and never sends an identifier the API would reject.
* Good, because identifiers stay masked in telemetry and logs, also in `IHttpClientFactory`'s entries.
* Good, because the flat-file package can reuse the models.
* Bad, because the provider adds seven public error types, an interface and the retry filter, frozen from their release.
* Bad, because an app plugs the retry filter into its handler itself (a few lines), instead of one call.
* Bad, because the date bound, the quota and the midnight block come from the statute and the ministry's pages, not from a contract: if the ministry changes them, local validation may reject a date the API accepts, or `RetryAfter` may be wrong, until a patch.
* Bad, because the 429 fixture is built from the error-code table, not captured.
* Bad, because WL2 reuses `api/`'s date bound, which the ministry doesn't document for WL2.

### Confirmation

* Unit tests per client method, every row of the error table, the validators (including indexed batch errors), the proof's DST rule, and `ShouldFollowTheMetadataRule()` for every error type; WireMock tests with captured fixtures for every error path of ADR-0002 plus `WL-191`, `WL-195`, `WL-197` and `WL-118`; explicit tests against the test host.
* Tests of the retry filter (yes for `WL-195`/`WL-196`, 408 and 503; no for 429, `WL-197` and other 4xx; the body is still readable afterwards).
* A test that `IHttpClientFactory`'s log entries carry no identifier (ADR-0017), and tests that spans and messages carry only masked identifiers.
* The `mf-white-list` and `mf-white-list-di` mutation shards at 100% (ADR-0020); the AOT smoke app calls each client offline (ADR-0014).
* The `reviewer` agent checks the package against this ADR and `/new-provider`.

## Pros and Cons of the Options

### One client for all endpoints

* Good, because there is one type to register and inject.
* Bad, because WL2 answers another question under another quota and with other errors, and an app that needs only the status would carry the whole search surface.

### An entity that isn't on the list as a success with an empty subject

* Good, because the proof stays on the success result without a special error.
* Bad, because every caller must check for an empty subject, unlike NBP's empty answers, and `EnsureSuccess()` wouldn't stop on it.

### `requestDateTime` as a `DateTime` with `DateTimeKind.Unspecified`

* Good, because it is what the API sends.
* Bad, because the caller must know it is Warsaw time; it is an instant, not a public date (ADR-0005).

### A retry extension on `HttpStandardResilienceOptions` in the DI package

* Good, because the app configures it with one call.
* Bad, because the DI package would reference `Microsoft.Extensions.Http.Resilience` (30 packages with its dependencies on net10.0, among them Polly, telemetry and configuration packages) for every app, also one that adds no handler, which ADR-0004 rules out.

### Validation by the server only

* Good, because the library can't reject what the API would accept.
* Bad, because each rejected request spends quota and sends an identifier to the ministry for nothing.

### WL2's error codes and "not in the register" as values of the status enum

* Good, because the enum mirrors the contract.
* Bad, because I, D and X aren't statuses, so a caller switching on the status would take a failed query for an answer; and B would be "not on the list" handled differently than on `api/`.

## More Information

* Sources, read on 2026-11-08: the [API page](https://www.gov.pl/web/kas/api-wykazu-podatnikow-vat) with the specification v1.6.2 of 05.11.2026, its [Swagger contract](https://www.gov.pl/attachment/a4ef85db-b8ae-47b0-becf-ca18673c8826), [description](https://www.gov.pl/attachment/6edec876-361c-4b01-b601-2c9257dc49d4), [error-code table](https://www.gov.pl/attachment/0e0722a4-6313-4259-9c6c-6a4b13a79800) and [test data](https://www.gov.pl/attachment/0daec8b8-5ff3-4e92-852d-b7a49fd80bff); the [FAQ](https://www.gov.pl/web/kas/pytania-i-odpowiedzi) and the [list's page](https://www.gov.pl/web/kas/wykaz-podatnikow-vat); the acts linked above (consolidated texts of 2025–2026). Live probes of the test and production hosts on the same day (the date bound, the batch limit and duplicates, the quota).
* Not documented by the ministry: whether a batch counts as one or as up to 30 of the 100 daily searches, the real body of a blocked response, the rules for querying too fast, and WL2's date range.
* Issue: [#212](https://github.com/milten89/OpenUrzednik.NET/issues/212). The maintainer settled the open decisions in chat on 2026-11-08.

### Corrections before release

* 2026-11-10 · [#258](https://github.com/milten89/OpenUrzednik.NET/issues/258) · The error types and `IWhiteListErrorCode` live in the root namespace `OpenUrzednik.MF.WhiteList`; the shared key `whiteListCode` is `WhiteListErrorMetadata.WhiteListCodeMetadataKey`, and `requestId` and `requestDateTime` are constants on `WhiteListNotFoundError`; `WhiteListCode` holds only a code of the documented shape (`WL-` and three digits, or one WL2 letter), else `null`; `ApiUrl` is the host root, and a path ending in `api/` or `api2/` is rejected. Reason: the skeleton's review placed the keys like Core's (a key one type adds is a constant on that type), kept a proxy's or a changed API's text out of messages and metadata, and caught a doubled `api/api/` path.
