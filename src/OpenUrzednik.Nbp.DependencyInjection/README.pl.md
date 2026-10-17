# OpenUrzednik.Nbp.DependencyInjection

🇬🇧 Pełna dokumentacja jest po angielsku: [README.md](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp.DependencyInjection/README.md). Ta wersja zawiera tylko część informacyjną. Jeśli wersje się różnią, obowiązuje angielska.

Rejestracja klientów **NBP** z [OpenUrzednik.Nbp](https://www.nuget.org/packages/OpenUrzednik.Nbp) w Microsoft.Extensions.DependencyInjection: klienty typowane z `IHttpClientFactory`, sprawdzane opcje, logowanie i śledzenie.

> Część zestawu [OpenUrzednik.NET](https://github.com/milten89/OpenUrzednik.NET). Przegląd projektu po polsku znajdziesz w [głównym README](https://github.com/milten89/OpenUrzednik.NET/blob/develop/README.pl.md).

## Instalacja

```bash
dotnet add package OpenUrzednik.Nbp.DependencyInjection
```

## Użycie

```csharp
builder.Services.AddOpenUrzednikNbp();
```

```csharp
public sealed class RatesService(INbpCurrencyExchangeRateClient rates)
{
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetUsdAsync(CancellationToken cancellationToken)
        => rates.GetLatestAsync("USD", cancellationToken: cancellationToken);
}
```

## Więcej po angielsku

- [Rejestracja, czas życia klientów i opcje](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp.DependencyInjection/README.md#usage)
- [Ponowienia i limity czasu](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp.DependencyInjection/README.md#retries-and-timeouts)
- [Logowanie i śledzenie](https://github.com/milten89/OpenUrzednik.NET/blob/develop/src/OpenUrzednik.Nbp.DependencyInjection/README.md#logging-and-tracing)
