---
name: verify-api
description: Compare provider DTOs and WireMock test payloads with live responses from the provider's real public API (NBP, and each provider that adds its notes here), and capture real responses as test fixtures. Use when adding or changing DTOs, when a WireMock payload was hand-written, or when the API might have changed.
argument-hint: "<provider> [endpoint path or DTO name]"
---

# Verify DTOs against the live API

Goal: our DTOs and test payloads must match what the API really returns, so that a "passing" WireMock test means the client works in production.

Read the provider's section under "Provider notes" first; a provider without one gets it in the same PR, from its provider ADR. Placeholders as in `/new-provider`: `<Package>` is the package id (`OpenUrzednik.Nbp`), `<Provider>` its folder in shared test projects (`Nbp`).

## Steps

1. **List the endpoints to check.** Take the provider's URL builders (or request builders) and the DTOs in `src/<Package>/Dto/`. Narrow to `$ARGUMENTS` if given.
2. **Call the real API** with no state-changing calls (`curl -s` for REST; a SOAP session is closed with its logout call), for every request shape the client builds and for the error cases the provider notes list. Record the status code **and the body** of each error.
   - Keep the request count low: these are public services. Don't loop or hammer them, and count calls against any daily quota the provider notes give; stop and ask before a call would use a scarce quota.
   - Prefer the provider's test environment when it has one. Never send, print or save a real API key, session id or token; take keys from the environment, not from files you'd commit.
   - Query with legal-person identifiers (companies), not a sole trader's NIP or a private account number, and write endpoints in the report as route templates (`search/nip/{nip}`): the report ends up in PRs (ADR-0025).
3. **Compare with the DTOs:**
   - A `required` property missing from any real response is a **bug**: deserialization will fail.
   - Report extra fields we ignore, type mismatches (number vs string, date formats) and casing.
4. **Compare with the WireMock tests** in `tests/OpenUrzednik.IntegrationTests/<Provider>/WireMock/`. Flag every hand-written body that differs in shape from the real one.
5. **Capture fixtures** when asked, or when fixing a mismatch: save real bodies under `tests/OpenUrzednik.IntegrationTests/<Provider>/Fixtures/<endpoint-shape>.<ext>` (trim long arrays to 2–3 items, keep the real structure) and load them in the WireMock tests instead of inline bodies. If responses can contain personal data, capture from the provider's test environment or from legal-person records, replace any remaining personal fields with fake values of the same format (fake identifiers must still pass their check digits: `src/Identifiers`), and never commit API keys, session ids or tokens. Hand-written bodies are only for cases the real API can't produce (an empty answer `[]`, malformed JSON).
6. **Report** a table: endpoint | status | fields in response | DTO verdict (OK / missing required / extra / type mismatch) | test payload verdict. List the limits you observed (max count, max date range, earliest date, quota) next to the values used in `Validation/`, and the number of calls you made.

Don't change validators based on one observation without saying so: point out differences from the documented limits and let the maintainer decide.

## Provider notes

### NBP (`OpenUrzednik.Nbp`)

- Builders: `src/OpenUrzednik.Nbp/UrlBuilder/NbpUrlBuilderFactory.cs`, `NbpUrlBuilder.cs`. Add `?format=json` to every call. No authentication, no quota, no test environment.
- Cover:
  - every table (`a`, `b`, `c`) for both `exchangerates/tables/{t}` and `exchangerates/rates/{t}/{code}`
  - `cenyzlota`
  - each URL shape: latest, `last/{n}`, `today`, `{date}`, `{from}/{to}`
  - error cases: a future date, an out-of-range `last/{n}` (> 255 for rates and gold prices, > 67 for tables A and C, > 14 for table B), a date with no publication (weekend), a too-long range. NBP returns plain-text messages such as `400 BadRequest - Błędny zakres dat`.

### MF White List (`OpenUrzednik.MF.WhiteList`)

- Hosts ([ADR-0023](../../../docs/adr/0023-mf-white-list-provider.md)): production `https://wykazpodatnikowvat.mf.gov.pl/` and the **test host** `https://wykazpodatnikowvat-test.mf.gov.pl/` (`WhiteListOptions.TestApiUrl`), each with `api/` (search, check) and `api2/` (WL2 status). The old hosts `wl-api.mf.gov.pl` and `wl-test.mf.gov.pl` work only until 2026-12-01. GET only, no authentication. Contract: the Swagger file and error-code table linked from ADR-0023's More Information (v1.6.2 of 05.11.2026).
- Prefer the test host. Its official test data (NIPs, REGONs and account numbers that return fictitious subjects) is the ministry's attachment <https://www.gov.pl/attachment/0daec8b8-5ff3-4e92-852d-b7a49fd80bff>. Query production only for a legal person (the Ministry of Finance: NIP `5260250274`, REGON `000002217`).
- Quota per IP: search 100 queries a day (of up to 30 subjects), check 5,000 entities, WL2 50,000 a day; an IP over the limit, or one that queries too fast, may be blocked until midnight (`WL-191`, 429), for the API and the web search alike. Not enforced on 2026-11-08 (130 test-host and 160 production single searches answered 200), but count every call and keep a session to a handful.
- Endpoints the client builds (route templates, as written in reports): `search/nip/{nip}`, `search/regon/{regon}`, `search/bank-account/{account}`, and the batches `search/nips/{nips}`, `search/regons/{regons}`, `search/bank-accounts/{accounts}` (1 to 30 normalized identifiers, comma-separated), each with `?date=yyyy-MM-dd` (required); the checks `check/nip/{nip}/bank-account/{account}` and `check/regon/{regon}/bank-account/{account}` (same `date`).
- Answers: `{"result": {"subject": {...} | null, "requestId", "requestDateTime"}}`, and `subjects: [...]` for an account. A batch: `{"result": {"entries": [...], "requestId", "requestDateTime"}}`, one entry per identifier sent, in the order sent, duplicates included; an entry is `{identifier, subjects: [...]}` (`[]` when not on the list) or `{identifier, error: {message, code}}`. `requestId` and `requestDateTime` (`dd-MM-yyyy HH:mm:ss`, Warsaw time, no offset) were in every captured 200. The contract requires only the subject's `name`; every other subject field is optional and nullable.
- Check answers (2026-11-21): `{"result": {"accountAssigned": "TAK" | "NIE", "requestId", "requestDateTime"}}`, all three in every captured 200. `NIE` also answers an entity that isn't on the list (NIP `5260250274` on the test host): a check has no not-found case. `TAK` can't come from the test host, because its subjects have no account numbers; it was captured from production for the Ministry of Finance (NIP `5260250274` or REGON `000002217` with account `59101010100075912222000000`). Errors as for the searches: `WL-115` invalid NIP, `WL-107` invalid REGON, `WL-111` invalid account, `WL-118` date before the range.
- Probe results (2026-11-20, test host unless noted):
  - Subject fields: `name`, `nip`, `statusVat` (`Czynny`, `Zwolniony`, `Niezarejestrowany`), `regon`, `pesel` (always `null`, not mapped), `krs`, `residenceAddress`, `workingAddress`, `representatives`/`authorizedClerks`/`partners` (`companyName`, `firstName`, `lastName`, `pesel`, `nip`), `registrationLegalDate`, `registrationDenialDate`, `registrationDenialBasis`, `restorationDate`, `restorationBasis`, `removalDate`, `removalBasis`, `exemptionSmeDate` (new in v1.6.2, "Data zwolnienia SME"), `accountNumbers`, `hasVirtualAccounts`. Dates are `yyyy-MM-dd`.
  - The test host sends `""` for an empty `*Basis`; production sends `null`. The mapper reads both as `null`.
  - Test NIP `3245174504` returns "Nazwa Firmy 1" (`Czynny`); 9-digit test REGONs `216973362` and `882837568` return subjects; the 14-digit test REGONs fail the API's own check (`WL-107`); the test subjects have no account numbers, and the test account numbers return `subjects: []`; `5260250274` isn't on the test host (`subject: null`).
  - Errors, all 400 with `{"message", "code"}`: `WL-101` no date, `WL-103` a future date, `WL-107` an invalid REGON, `WL-111` an invalid account number, `WL-115` an invalid NIP, `WL-118` a date before 1 January five years back. The client rejects all of them before sending.
  - Batches (2026-11-21): an invalid identifier is an entry `error` in a 200 (`WL-115` NIP, `WL-107` REGON, `WL-111` account, `WL-113` a wrong length), not a 400; 31 identifiers are a 400 `WL-130` ("Przekroczono maksymalną liczbę argumentów zapytania."). An empty batch (`search/nips/?date=…`) is blocked by the ministry's firewall (Imperva), which answers **200 with an HTML page**, not JSON: never probe it, and the client never sends one (an empty list is a local `ValidationError<int>`).
- Fixtures in `tests/OpenUrzednik.IntegrationTests/MF/WhiteList/Fixtures` (provenance in `WhiteListFixtures.cs` there): the test host for `search-nip*.json`, `search-bank-account-not-found.json` and `error-400-wl-1xx.json`, and (2026-11-21) the batches `search-nips.json`, `search-regons.json`, `search-bank-accounts.json` (each with an entry not on the list and an entry `error`) and `error-400-wl-130.json`; production for the Ministry of Finance in `search-regon.json` and `search-bank-account.json`; the checks (2026-11-21): the test host for `check-nip-not-assigned.json`, `check-regon-not-assigned.json` and `check-nip-not-on-list.json` (all NIE), production for the TAK answers `check-nip-assigned.json` and `check-regon-assigned.json`; `*.from-error-table.json` (`WL-191` 429, `WL-195` 400, `WL-197` 500) are built from the error-code table, not captured, until a real one is.
