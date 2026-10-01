using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Currency;

internal static class Mapper
{
    internal static CurrencyExchangeRates MapToCurrencyExchangeRates(CurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyExchangeRate(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    internal static ExchangeRate MapToCurrencyExchangeRate(CurrencyExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        return new ExchangeRate(dto.TableId, dto.PublicationDate, dto.Price);
    }

    internal static BuySellExchangeRates MapToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new BuySellExchangeRate[dto.Rates.Length];
        for (var i = 0; i < rates.Length; i++)
            rates[i] = MapToBuySellExchangeRate(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    internal static BuySellExchangeRate MapToBuySellExchangeRate(BuySellCurrencyExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        return new BuySellExchangeRate(dto.TableId, dto.PublicationDate, dto.Buy, dto.Sell);
    }
}
