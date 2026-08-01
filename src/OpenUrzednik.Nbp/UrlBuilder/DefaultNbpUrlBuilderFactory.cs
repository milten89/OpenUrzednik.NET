using System.Collections.Concurrent;

namespace OpenUrzednik.Nbp.UrlBuilder;

public class DefaultNbpUrlBuilderFactory : INbpUrlBuilderFactory
{
    private static readonly ConcurrentDictionary<string, INbpUrlBuilder> Cache = new();

    public INbpUrlBuilder GetTableBuilder(NbpTable table)
        => GetOrAdd($"exchangerates/tables/{MapToUrl(table)}");

    public INbpUrlBuilder GetCurrencyBuilder(NbpTable table, string currency)
        => GetOrAdd($"exchangerates/rates/{MapToUrl(table)}/{Uri.EscapeDataString(currency)}");

    public INbpUrlBuilder GetGoldBuilder() 
        => GetOrAdd("cenyzlota");    

    private static string MapToUrl(NbpTable table)
    {
        return table switch
        {
            NbpTable.A => "a",
            NbpTable.B => "b",
            NbpTable.C => "c",
            _ => throw new ArgumentException($"'{nameof(table)}' has value not defined by {nameof(NbpTable)}."),
        };
    }

    private static INbpUrlBuilder GetOrAdd(string relativeUrl) 
        => Cache.GetOrAdd(relativeUrl, url => new DefaultNbpUrlBuilder(url));
}