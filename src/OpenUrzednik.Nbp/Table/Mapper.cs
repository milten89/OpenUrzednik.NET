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
        NbpPayload.EnsurePresent(dto, "item");

        var tables = new ExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToExchangeRateTable(dto[i]);

        return Array.AsReadOnly(tables);
    }

    internal static ExchangeRateTable MapToExchangeRateTable(ExchangeRateTableDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new TableRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToTableRate(dto.Rates[i]);

        return new(dto.TableId, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    internal static TableRate MapToTableRate(ExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new TableRate(dto.CurrencyName, dto.CurrencyCode, dto.Price);
    }

    internal static IReadOnlyList<BuySellExchangeRateTable> MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        var tables = new BuySellExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToBuySellExchangeRateTable(dto[i]);

        return Array.AsReadOnly(tables);
    }

    internal static BuySellExchangeRateTable MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new TableBuySellRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToTableBuySellRate(dto.Rates[i]);

        return new(dto.TableId, dto.TradingDate, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    internal static TableBuySellRate MapToTableBuySellRate(BuySellExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new TableBuySellRate(dto.CurrencyName, dto.CurrencyCode, dto.Ask, dto.Bid);
    }
}
