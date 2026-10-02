using System.Collections.Concurrent;

namespace OpenUrzednik.Nbp.UrlBuilder;

/// <summary>
/// Default <see cref="INbpUrlBuilderFactory"/>: builds the request paths of the public NBP API.
/// Builders are cached per factory instance, so each client keeps its own.
/// </summary>
public class NbpUrlBuilderFactory : INbpUrlBuilderFactory
{
    private readonly ConcurrentDictionary<string, INbpUrlBuilder> _cache = new();

    /// <inheritdoc/>
    public INbpUrlBuilder GetTableBuilder(NbpTable table)
        => GetOrAdd($"exchangerates/tables/{MapToUrl(table)}");

    /// <inheritdoc/>
    public INbpUrlBuilder GetCurrencyBuilder(NbpTable table, string currency)
        => GetOrAdd($"exchangerates/rates/{MapToUrl(table)}/{Uri.EscapeDataString(currency)}");

    /// <inheritdoc/>
    public INbpUrlBuilder GetGoldBuilder()
        => GetOrAdd("cenyzlota");

    private static string MapToUrl(NbpTable table)
    {
        return table switch
        {
            NbpTable.A => "a",
            NbpTable.B => "b",
            NbpTable.C => "c",
            _ => throw new ArgumentException($"'{nameof(table)}' has value not defined by {nameof(NbpTable)}.", nameof(table)),
        };
    }

    private INbpUrlBuilder GetOrAdd(string relativeUrl)
        => _cache.GetOrAdd(relativeUrl, url => new NbpUrlBuilder(url));
}
