using OpenUrzednik.Nbp.Currency;

namespace OpenUrzednik.Nbp.Tests.Currency;

// Shared helpers; tests are in one file per method.
public sealed partial class CurrencyExchangeRatesTest
{
    // Each call builds a new list, so equality can't come from the list reference.
    private static CurrencyExchangeRates Create()
        => new("dolar amerykański", "USD",
        [
            new CurrencyRate("001/A/NBP/2026", new DateTime(2026, 1, 2), 3.6015m),
            new CurrencyRate("002/A/NBP/2026", new DateTime(2026, 1, 5), 3.6122m),
        ]);
}
