# OpenUrzednik.Core

🇬🇧 [English version](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.en.md)

Wspólne abstrakcje dla całego zestawu **OpenUrzednik.NET** — wzorzec Result (`OpenUrzednikResult` / `OpenUrzednikResult<T>`), typowane błędy (`OpenUrzednikError`) oraz wyjątki (`OpenUrzednikException`) dla konsumentów preferujących klasyczny styl.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET) — zobacz [główne README](https://github.com/milten89/OpenUrzednik.NET#readme) po pełny przegląd projektu.

## Instalacja

```bash
dotnet add package OpenUrzednik.Core
```

## Przykład użycia

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;

OpenUrzednikResult<int> result = OpenUrzednikResult.Success(42);
int value = result.EnsureSuccess();
```

## Praca z wynikiem

```csharp
var label = result
    .Map(v => v * 2)
    .Match(v => $"Wartość: {v}", errors => $"Błąd: {errors[0].Message}");

if (result.TryGetValue(out var current))
    Console.WriteLine(current);
```

`Map` zmienia wartość udanego wyniku, `Bind` uruchamia kolejną operację, która też zwraca wynik, a `Match` obsługuje oba przypadki. Jeśli wynik zawiera błędy, przechodzą one dalej bez zmian, a przekazane funkcje nie są wywoływane.

`EnsureSuccess()` rzuca wyjątek pochodny od `OpenUrzednikException`. Jego właściwość `Error` zawiera błąd razem z metadanymi (np. kodem statusu HTTP), a `Errors` wszystkie błędy wyniku. Kilka błędów walidacji trafia do jednego `ValidationException`.

Ten pakiet jest zależnością wszystkich pakietów provider-specific (`OpenUrzednik.Gus`, `OpenUrzednik.Krs`, `OpenUrzednik.Mf`, `OpenUrzednik.Nbp`) i zwykle nie instaluje się go samodzielnie, chyba że budujesz własnego klienta w tym samym stylu.
