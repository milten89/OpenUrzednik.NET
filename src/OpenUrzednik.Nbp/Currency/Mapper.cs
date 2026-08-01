using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Currency;

internal static class Mapper
{
    internal static CountryExchangeRates MapToCountryExchangeRates(CountryExchangeRatesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        
        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyExchangeRateDto(dto.Rates[i]);

        return new(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode, Array.AsReadOnly(rates));
    }
    
    internal static CurrencyExchangeRates MapToCurrencyExchangeRates(CurrencyExchangeRatesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyExchangeRateDto(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }
    
    internal static ExchangeRate MapToCurrencyExchangeRateDto(CurrencyExchangeRateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ExchangeRate(dto.TableId, dto.PublicationDate, dto.Price);
    }

    internal static BuySellExchangeRates MapToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

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