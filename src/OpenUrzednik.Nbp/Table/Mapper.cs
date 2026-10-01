using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Table;

internal static class Mapper
{
    internal static NbpTable MapToNbpTable(TableType table)
    {
        return table switch
        {
            TableType.A => NbpTable.A,
            TableType.B => NbpTable.B,
            _ => throw new ArgumentException($"'{nameof(table)}' has value not defined by {nameof(TableType)}.", nameof(table)),
        };
    }

    internal static IReadOnlyList<ExchangeRateTable> MapToExchangeRateTable(ExchangeRateTableDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        var tables = new ExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToExchangeRateTable(dto[i]);

        return Array.AsReadOnly(tables);
    }

    internal static ExchangeRateTable MapToExchangeRateTable(ExchangeRateTableDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToExchangeRate(dto.Rates[i]);

        return new(dto.TableId, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    internal static ExchangeRate MapToExchangeRate(ExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        return new ExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Price);
    }

    internal static IReadOnlyList<BuySellExchangeRateTable> MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        var tables = new BuySellExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToBuySellExchangeRateTable(dto[i]);

        return Array.AsReadOnly(tables);
    }

    internal static BuySellExchangeRateTable MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new BuySellExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToBuySellExchangeRateTable(dto.Rates[i]);

        return new(dto.TableId, dto.TradingDate, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    internal static BuySellExchangeRate MapToBuySellExchangeRateTable(BuySellExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        return new BuySellExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Buy, dto.Sell);
    }
}
