# OpenUrzednik.Core

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Narodowym Bankiem Polskim (NBP) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Wspólne abstrakcje dla całego zestawu **OpenUrzednik.NET** — wzorzec Result (`OpenUrzednikResult` / `OpenUrzednikResult<T>`), typowane błędy (`OpenUrzednikError`) oraz wyjątki (`OpenUrzednikException`) dla konsumentów preferujących klasyczny styl.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.Core
```

## Przykład użycia

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;

OpenUrzednikResult<int> result = OpenUrzednikResult.Success(42);
int value = result.EnsureSuccess();
```

Wynik z błędem:

```csharp
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

OpenUrzednikResult<int> failed = OpenUrzednikResult.Failure<int>(new ValidationError("NIP is invalid.", ruleName: "nipChecksum", name: "nip", value: null));

try
{
    int value = failed.EnsureSuccess();
}
catch (OpenUrzednikValidationException ex)
{
    Console.WriteLine(ex.Message);
}
```

## Więcej po angielsku

- [Praca z wynikiem: `Map`, `Bind`, `Match`, wyjątki](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md#working-with-a-result)
- [Błędy: typy, kody i metadane](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Core/README.md#errors)
