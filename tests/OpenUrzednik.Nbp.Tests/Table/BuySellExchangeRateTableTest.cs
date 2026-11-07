using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Tests.Table;

// Shared helpers; tests are in one file per method.
public sealed partial class BuySellExchangeRateTableTest
{
    // Each call builds a new list, so equality can't come from the list reference.
    private static BuySellExchangeRateTable Create()
        => new("001/C/NBP/2026", new DateTime(2025, 12, 31), new DateTime(2026, 1, 2),
        [
            new TableBuySellRate("dolar amerykański", "USD", 3.6735m, 3.6007m),
            new TableBuySellRate("euro", "EUR", 4.2955m, 4.2105m),
        ]);
}
