using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Currency;

internal static class Mapper
{
    internal static CountryExchangeRates MapToCountryExchangeRates(CountryExchangeRatesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Rates is null)
            throw new ArgumentException($"{nameof(dto.Rates)} cannot be null.", nameof(dto));
        
        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyExchangeRate(dto.Rates[i]);

        return new(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode, Array.AsReadOnly(rates));
    }
    
    internal static CurrencyExchangeRates MapToCurrencyExchangeRates(CurrencyExchangeRatesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Rates is null)
            throw new ArgumentException($"{nameof(dto.Rates)} cannot be null.", nameof(dto));

        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyExchangeRate(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }
    
    internal static ExchangeRate MapToCurrencyExchangeRate(CurrencyExchangeRateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ExchangeRate(dto.TableId, dto.PublicationDate, dto.Price);
    }

    internal static BuySellExchangeRates MapToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Rates is null)
            throw new ArgumentException($"{nameof(dto.Rates)} cannot be null.", nameof(dto));

        var rates = new BuySellExchangeRate[dto.Rates.Length];
        for (var i = 0; i < rates.Length; i++)
            rates[i] = MapToBuySellExchangeRate(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    internal static BuySellExchangeRate MapToBuySellExchangeRate(BuySellCurrencyExchangeRateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuySellExchangeRate(dto.TableId, dto.PublicationDate, dto.Buy, dto.Sell);
    }
}