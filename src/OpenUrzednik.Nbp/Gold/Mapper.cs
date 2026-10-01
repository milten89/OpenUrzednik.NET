using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Gold;

internal static class Mapper
{
    internal static IReadOnlyList<GoldPrice> MapToGoldPrice(GoldPriceDto[] dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        var goldPrices = new GoldPrice[dto.Length];
        for (int i = 0; i < goldPrices.Length; i++)
            goldPrices[i] = MapToGoldPrice(dto[i]);

        return Array.AsReadOnly(goldPrices);
    }

    internal static GoldPrice MapToGoldPrice(GoldPriceDto dto)
    {
        NbpPayload.EnsurePresent(dto, nameof(dto));

        return new GoldPrice(dto.Date, dto.Price);
    }
}
