# Contributing to OpenUrzednik.NET

🇬🇧 [English version](CONTRIBUTING.en.md)

## Zanim zaczniesz

- Sprawdź [issues](../../issues) — może ktoś już nad tym pracuje.
- Do większych zmian (nowy pakiet, zmiana publicznego API) najpierw otwórz issue z propozycją, zanim napiszesz kod — oszczędzi to czas obu stronom.

## Konwencje w repo

Wiążące decyzje architektoniczne są opisane w [`docs/adr/`](docs/adr/README.md), a aktualne priorytety w [`docs/BACKLOG.md`](docs/BACKLOG.md). Zmiana, która jest sprzeczna z zaakceptowanym ADR, wymaga nowego ADR w tym samym PR.

- **`OpenUrzednikResult` / `OpenUrzednikResult<T>` zamiast rozproszonej logiki błędów** dla wszystkich błędów, które mogą wystąpić podczas normalnego działania (walidacja, statusy HTTP, błędne odpowiedzi, błędy sieci, timeouty). Wyjątki rzucamy tylko przy anulowaniu przez wywołującego, błędach programisty (np. `null` w argumencie) i błędach krytycznych. Nie używaj `catch (Exception)` ([ADR-0002](docs/adr/0002-result-pattern-and-error-handling.md)). Wyjątki (`OpenUrzednikException` i jego pochodne) są dostępne przez `.EnsureSuccess()` / `.EnsureSuccessAsync()` dla konsumentów preferujących klasyczny styl — nie dodawaj równoległych wariantów API.
- **Zależności:** pakiety Core i providerów nie mają zależności NuGet (poza oficjalnymi pakietami BCL dla netstandard2.0); integracje z `Microsoft.Extensions.*` trafiają do osobnych pakietów ([ADR-0004](docs/adr/0004-dependency-policy.md)).
- **`OpenUrzednikError`** jest bazowym typem dla błędów przewidywalnych. Każdy konkretny błąd powinien dziedziczyć po nim i implementować `Code` oraz `CreateException()`, które zwraca wyjątek pochodny od `OpenUrzednikException`.
- **Pakiety provider-specific** powinny być zgodne z ruchem przyjętym w `OpenUrzednik.Core` — nazwy typów, nazewnictwo błędów i sposób zwracania rezultatów mają być spójne.
- **Publiczne API** dokumentuj komentarzami `///` — `GenerateDocumentationFile` jest włączone, więc trafiają do IntelliSense konsumenta.
- **Testy** nie mogą zależeć od prawdziwych serwerów urzędów w domyślnym przebiegu CI. Scenariusze HTTP testuj stubami (`StubHttpMessageHandler`) i WireMockiem, używając odpowiedzi przechwyconych z prawdziwego API. Testy wywołujące prawdziwe API oznaczaj `[ManualFact]` / `[ManualTheory]` — uruchamiają się tylko na żądanie: `dotnet test tests/OpenUrzednik.IntegrationTests --explicit on` albo z ustawioną zmienną `OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED`.

## Zgłaszanie luk bezpieczeństwa

Patrz [SECURITY.md](SECURITY.md) — nie zgłaszaj podatności przez publiczne issue.

## Pull requesty

1. Fork + branch od `develop` (`feature/…`, `fix/…`, `docs/…`, `chore/…`).
2. PR kieruj do `develop` — zmiany są łączone przez *squash*. Gałąź `main` przyjmuje tylko PR-y wydaniowe z `develop` oraz hotfixy (zob. [`docs/RELEASE-PROCESS.md`](docs/RELEASE-PROCESS.md)).
3. Przed otwarciem PR uruchom lokalnie `dotnet test OpenUrzednik.slnx` oraz `dotnet format OpenUrzednik.slnx --verify-no-changes`.
4. Opisz *co* i *dlaczego*, nie tylko *jak* — szczególnie przy zmianach w `Core`. Opis PR i komunikaty commitów piszemy po angielsku ([ADR-0009](docs/adr/0009-documentation-language.md)).

## Praca z agentami AI

Instrukcje dla agentów (Claude Code, Copilot itp.) znajdują się w [`CLAUDE.md`](CLAUDE.md) / [`AGENTS.md`](AGENTS.md).
