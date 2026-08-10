using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Gold;

internal static class Mapper
{
    internal static GoldPrice[] MapToGoldPrice(GoldPriceDto[] dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var goldPrices = new GoldPrice[dto.Length];
        for (int i = 0; i < goldPrices.Length; i++)
            goldPrices[i] = MapToGoldPrice(dto[i]);

        return goldPrices;
    }

    internal static GoldPrice MapToGoldPrice(GoldPriceDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new GoldPrice(dto.Date, dto.Price);
    }
}
