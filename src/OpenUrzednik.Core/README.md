# OpenUrzednik.Core

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

Ten pakiet jest zależnością wszystkich pakietów provider-specific (`OpenUrzednik.Gus`, `OpenUrzednik.Krs`, `OpenUrzednik.Mf`, `OpenUrzednik.Nbp`) i zwykle nie instaluje się go samodzielnie, chyba że budujesz własnego klienta w tym samym stylu.
