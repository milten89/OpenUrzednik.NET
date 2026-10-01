using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Currency;

internal static class Mapper
{
    internal static CurrencyExchangeRates MapToCurrencyExchangeRates(CurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new CurrencyRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyRate(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    internal static CurrencyRate MapToCurrencyRate(CurrencyExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new CurrencyRate(dto.TableId, dto.PublicationDate, dto.Price);
    }

    internal static BuySellExchangeRates MapToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new CurrencyBuySellRate[dto.Rates.Length];
        for (var i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyBuySellRate(dto.Rates[i]);

        return new(dto.CurrencyName, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    internal static CurrencyBuySellRate MapToCurrencyBuySellRate(BuySellCurrencyExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new CurrencyBuySellRate(dto.TableId, dto.PublicationDate, dto.Buy, dto.Sell);
    }
}
