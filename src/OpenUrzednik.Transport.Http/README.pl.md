# OpenUrzednik.Transport.Http

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Transport.Http/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Narodowym Bankiem Polskim (NBP) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Typy transportu HTTP w **OpenUrzednik.NET**: błędy, które klient REST zwraca, gdy spowodowała je odpowiedź HTTP, i ich kod statusu.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/main/README.pl.md).

## Instalacja

Każdy dostawca REST (np. `OpenUrzednik.Nbp`) zależy od tego pakietu, więc dostajesz go razem z dostawcą. Osobno instalujesz go tylko wtedy, gdy piszesz własnego klienta w tym samym stylu:

```bash
dotnet add package OpenUrzednik.Transport.Http
```

## Użycie

```csharp
using System.Net;

using OpenUrzednik.Transport.Http;

var result = await client.GetAsync(date);
if (result.IsFailure && result.Errors[0] is IOpenUrzednikHttpStatusCode http)
    Console.WriteLine($"HTTP {(int)http.StatusCode}");

// Albo bez sprawdzania typu: null, gdy błędu nie spowodowała odpowiedź HTTP.
HttpStatusCode? status = result.Errors[0].GetHttpStatus();
```

## Więcej po angielsku

- [Które błędy odpowiadają którym statusom](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Transport.Http/README.md#usage)
- [Zgodność](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Transport.Http/README.md#compatibility)
- [Zależności](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.Transport.Http/README.md#dependencies)
