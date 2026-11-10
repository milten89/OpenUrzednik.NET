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

    /// <summary>The table with the latest publication date, or <see langword="null"/> when <paramref name="dto"/> is empty.</summary>
    internal static ExchangeRateTable? MapLastToExchangeRateTable(ExchangeRateTableDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        var last = NbpPayload.Latest(dto, static x => x.PublicationDate);
        return last is null ? null : MapToExchangeRateTable(last);
    }

    internal static ExchangeRateTable MapToExchangeRateTable(ExchangeRateTableDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new TableRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToTableRate(dto.Rates[i]);

        return new(dto.TableId, dto.PublicationDate.ToPublicDate(), Array.AsReadOnly(rates));
    }

    internal static TableRate MapToTableRate(ExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new TableRate(dto.CurrencyName ?? string.Empty, dto.CurrencyCode, dto.Price);
    }

    internal static IReadOnlyList<BuySellExchangeRateTable> MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        var tables = new BuySellExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToBuySellExchangeRateTable(dto[i]);

        return Array.AsReadOnly(tables);
    }

    /// <summary>The table with the latest publication date, or <see langword="null"/> when <paramref name="dto"/> is empty.</summary>
    internal static BuySellExchangeRateTable? MapLastToBuySellExchangeRateTable(BuySellExchangeRateTableDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        var last = NbpPayload.Latest(dto, static x => x.PublicationDate);
        return last is null ? null : MapToBuySellExchangeRateTable(last);
    }

    internal static BuySellExchangeRateTable MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new TableBuySellRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToTableBuySellRate(dto.Rates[i]);

        return new(dto.TableId, dto.TradingDate.ToPublicDate(), dto.PublicationDate.ToPublicDate(), Array.AsReadOnly(rates));
    }

    internal static TableBuySellRate MapToTableBuySellRate(BuySellExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new TableBuySellRate(dto.CurrencyName ?? string.Empty, dto.CurrencyCode, dto.Ask, dto.Bid);
    }
}
