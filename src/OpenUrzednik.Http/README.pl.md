# OpenUrzednik.Http

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Http/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

Wspólna warstwa HTTP dla pakietów **OpenUrzednik.NET**, które korzystają z API typu REST/JSON. Wysyła zapytania, zamienia statusy HTTP na błędy `OpenUrzednikResult`, odróżnia przekroczenie czasu od anulowania i zapisuje logi oraz spany.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Dla kogo

Pakiet jest dla autorów pakietów providerów (np. `OpenUrzednik.Nbp`). W aplikacji go nie potrzebujesz: instaluje się razem z providerem, a klienty providera ukrywają go za swoim API. Jego typy są w przestrzeni nazw `OpenUrzednik.Http.Infrastructure` ([ADR-0006](https://github.com/milten89/OpenUrzednik.NET/blob/develop/docs/adr/0006-shared-http-layer.md)).

## Więcej po angielsku

- [Przykład i zachowanie przy błędach](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Http/README.md#example)
