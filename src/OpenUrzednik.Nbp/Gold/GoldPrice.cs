namespace OpenUrzednik.Nbp.Gold;

/// <summary>
/// Gold price
/// </summary>
/// <param name="Date">Date, at 00:00 with <see cref="DateTimeKind.Unspecified"/></param>
/// <param name="Price">Gold price per gram</param>
public sealed record GoldPrice(DateTime Date, decimal Price);
