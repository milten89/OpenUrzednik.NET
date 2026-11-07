using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Tests.Table;

// Shared helpers; tests are in one file per method.
public sealed partial class ExchangeRateTableTest
{
    // Each call builds a new list, so equality can't come from the list reference.
    private static ExchangeRateTable Create()
        => new("001/A/NBP/2026", new DateTime(2026, 1, 2),
        [
            new TableRate("dolar amerykański", "USD", 3.6015m),
            new TableRate("euro", "EUR", 4.2187m),
        ]);
}
