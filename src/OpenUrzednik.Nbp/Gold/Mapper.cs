using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Gold;

internal static class Mapper
{
    internal static IReadOnlyList<GoldPrice> MapToGoldPrice(GoldPriceDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        var goldPrices = new GoldPrice[dto.Length];
        for (int i = 0; i < goldPrices.Length; i++)
            goldPrices[i] = MapToGoldPrice(dto[i]);

        return Array.AsReadOnly(goldPrices);
    }

    /// <summary>The price with the latest date, or <see langword="null"/> when <paramref name="dto"/> is empty.</summary>
    internal static GoldPrice? MapLastToGoldPrice(GoldPriceDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        var last = NbpPayload.Latest(dto, static x => x.Date);
        return last is null ? null : MapToGoldPrice(last);
    }

    internal static GoldPrice MapToGoldPrice(GoldPriceDto dto)
    {
        NbpPayload.EnsurePresent(dto, "item");

        return new GoldPrice(dto.Date.ToPublicDate(), dto.Price);
    }
}
