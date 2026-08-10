using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Table;

internal static class Mapper
{
    internal static NbpTable MapToNbpTable(MidTableType table)
    {
        return table switch
        {
            MidTableType.A => NbpTable.A,
            MidTableType.B => NbpTable.B,
            _ => throw new ArgumentException($"'{nameof(table)}' has value not defined by {nameof(MidTableType)}.", nameof(table)),
        };
    }

    internal static ExchangeRateTable[] MapToExchangeRateTable(ExchangeRateTableDto[] dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var tables = new ExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToExchangeRateTable(dto[i]);

        return tables;
    }

    internal static ExchangeRateTable MapToExchangeRateTable(ExchangeRateTableDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Rates is null)
            throw new ArgumentException($"'{nameof(dto.Rates)}' cannot be null.", nameof(dto));

        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToExchangeRate(dto.Rates[i]);

        return new(dto.TableId, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    internal static ExchangeRate MapToExchangeRate(ExchangeRateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Price);
    }

    internal static BuySellExchangeRateTable[] MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto[] dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var tables = new BuySellExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToBuySellExchangeRateTable(dto[i]);

        return tables;
    }

    internal static BuySellExchangeRateTable MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Rates is null)
            throw new ArgumentException($"'{nameof(dto.Rates)}' cannot be null.", nameof(dto));

        var rates = new BuySellExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToBuySellExchangeRateTable(dto.Rates[i]);

        return new(dto.TableId, dto.TradingDate, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    internal static BuySellExchangeRate MapToBuySellExchangeRateTable(BuySellExchangeRateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuySellExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Buy, dto.Sell);
    }
}
