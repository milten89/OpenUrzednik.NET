namespace OpenUrzednik.Nbp.Gold;

/// <summary>
/// Gold price
/// </summary>
/// <param name="Date">Date</param>
/// <param name="Price">Gold price per gram</param>
public sealed record GoldPrice(DateOnly Date, decimal Price);
