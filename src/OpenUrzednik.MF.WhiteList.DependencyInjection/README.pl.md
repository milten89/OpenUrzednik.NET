# OpenUrzednik.MF.WhiteList.DependencyInjection

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

> ⚠️ **Projekt nieoficjalny:** OpenUrzednik.NET to projekt społecznościowy. Nie jest powiązany z Ministerstwem Finansów, Krajową Administracją Skarbową (KAS) ani z żadną inną polską instytucją publiczną i nie działa z ich upoważnienia.

Rejestruje klienty **Białej Listy VAT** z pakietu [OpenUrzednik.MF.WhiteList](https://www.nuget.org/packages/OpenUrzednik.MF.WhiteList) w Microsoft.Extensions.DependencyInjection: klienty z `IHttpClientFactory`, sprawdzane opcje, logowanie, śledzenie i metryki.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/main/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.MF.WhiteList.DependencyInjection
```

## Użycie

```csharp
builder.Services.AddOpenUrzednikMFWhiteList();
```

```csharp
public sealed class PaymentCheck(IWhiteListClient whiteList)
{
    public Task<OpenUrzednikResult<AccountCheckResult>> CheckAsync(string nip, string account, DateTime date, CancellationToken cancellationToken)
        => whiteList.CheckNipBankAccountAsync(nip, account, date, cancellationToken);
}
```

Metoda rejestruje `IWhiteListClient` i `IWhiteListStatusClient`. Pakiet nie dodaje ponawiania zapytań ani limitów czasu: dodajesz je sam, np. `.AddStandardResilienceHandler()`.

## Więcej po angielsku

- [Opcje i rejestracja kluczowana (`AddKeyedOpenUrzednikMFWhiteList`)](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.md#several-white-list-setups)
- [Logi bez identyfikatorów](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.md#logs-without-identifiers)
- [Ponawianie zapytań, w tym filtr `WhiteListRetry` i wariant dla .NET Framework](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.md#resilience)
- [Logowanie i śledzenie](https://github.com/milten89/OpenUrzednik.NET/blob/main/src/OpenUrzednik.MF.WhiteList.DependencyInjection/README.md#logging-and-tracing)
