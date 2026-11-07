using OpenUrzednik.Nbp.Currency;

namespace OpenUrzednik.Nbp.Tests.Currency;

// Shared helpers; tests are in one file per method.
public sealed partial class BuySellExchangeRatesTest
{
    // Each call builds a new list, so equality can't come from the list reference.
    private static BuySellExchangeRates Create()
        => new("dolar amerykański", "USD",
        [
            new CurrencyBuySellRate("001/C/NBP/2026", new DateTime(2026, 1, 2), 3.6735m, 3.6007m),
            new CurrencyBuySellRate("002/C/NBP/2026", new DateTime(2026, 1, 5), 3.6842m, 3.6112m),
        ]);
}
