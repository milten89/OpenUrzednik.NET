# Contributing to OpenUrzednik.NET

## Zanim zaczniesz

- Sprawdź [issues](../../issues) — może ktoś już nad tym pracuje.
- Do większych zmian (nowy pakiet, zmiana publicznego API) najpierw otwórz issue z propozycją, zanim napiszesz kod — oszczędzi to czas obu stronom.

## Konwencje w repo

- **`OpenUrzednikResult` / `OpenUrzednikResult<T>` zamiast rozproszonej logiki błędów** dla błędów przewidywalnych (np. brak zasobu, błędna walidacja, przekroczenie limitu zapytań). Wyjątki (`OpenUrzednikException` i jego pochodne) są dostępne przez `.EnsureSuccess()` / `.EnsureSuccessAsync()` dla konsumentów preferujących klasyczny styl — nie dodawaj równoległych wariantów API.
- **`OpenUrzednikError`** jest bazowym typem dla błędów przewidywalnych. Każdy konkretny błąd powinien dziedziczyć po nim i implementować `Code` oraz `ToException()`.
- **Pakiety provider-specific** powinny być zgodne z ruchem przyjętym w `OpenUrzednik.Core` — nazwy typów, nazewnictwo błędów i sposób zwracania rezultatów mają być spójne.
- **Publiczne API** dokumentuj komentarzami `///` — `GenerateDocumentationFile` jest włączone, więc trafiają do IntelliSense konsumenta.
- **Testy** powinny unikać zależności od prawdziwych serwerów urzędów. Jeśli dodajesz scenariusze HTTP, używaj test doubles / stubów i trzymaj je z dala od domyślnego przebiegu CI.

## Zgłaszanie luk bezpieczeństwa

Patrz [SECURITY.md](SECURITY.md) — nie zgłaszaj podatności przez publiczne issue.

## Pull requesty

1. Fork + branch od `main`.
2. Przed otwarciem PR uruchom lokalnie `dotnet test OpenUrzednik.slnx`.
3. Opisz *co* i *dlaczego*, nie tylko *jak* — szczególnie przy zmianach w `Core`.
