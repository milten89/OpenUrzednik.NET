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
   - Query with legal-person identifiers (companies), not a sole trader's NIP or a private account number, and write endpoints in the report as route templates (`search/nip/{nip}`): the report ends up in PRs (ADR-0003).
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
