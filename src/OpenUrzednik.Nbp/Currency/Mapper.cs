using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;

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

        return new(dto.CurrencyName ?? string.Empty, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    /// <summary>
    /// The currency with only the rate that has the latest publication date, or <see langword="null"/> when <c>rates</c> is empty.
    /// </summary>
    internal static CurrencyExchangeRates? MapLastToCurrencyExchangeRates(CurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var last = NbpPayload.Latest(dto.Rates, static x => x.PublicationDate);
        return last is null ? null : new(dto.CurrencyName ?? string.Empty, dto.CurrencyCode, Array.AsReadOnly(new[] { MapToCurrencyRate(last) }));
    }

    internal static CurrencyRate MapToCurrencyRate(CurrencyExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new CurrencyRate(dto.TableId, dto.PublicationDate.ToPublicDate(), dto.Price);
    }

    internal static BuySellExchangeRates MapToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var rates = new CurrencyBuySellRate[dto.Rates.Length];
        for (var i = 0; i < rates.Length; i++)
            rates[i] = MapToCurrencyBuySellRate(dto.Rates[i]);

        return new(dto.CurrencyName ?? string.Empty, dto.CurrencyCode, Array.AsReadOnly(rates));
    }

    /// <summary>
    /// The currency with only the rate that has the latest publication date, or <see langword="null"/> when <c>rates</c> is empty.
    /// </summary>
    internal static BuySellExchangeRates? MapLastToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");
        NbpPayload.EnsurePresent(dto.Rates, "rates");

        var last = NbpPayload.Latest(dto.Rates, static x => x.PublicationDate);
        return last is null ? null : new(dto.CurrencyName ?? string.Empty, dto.CurrencyCode, Array.AsReadOnly(new[] { MapToCurrencyBuySellRate(last) }));
    }

    internal static CurrencyBuySellRate MapToCurrencyBuySellRate(BuySellCurrencyExchangeRateDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new CurrencyBuySellRate(dto.TableId, dto.PublicationDate.ToPublicDate(), dto.Ask, dto.Bid);
    }
}
