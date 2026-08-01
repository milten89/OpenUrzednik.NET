namespace OpenUrzednik.Nbp.UrlBuilder;

public interface INbpUrlBuilderFactory
{
    INbpUrlBuilder GetTableBuilder(NbpTable table);
    INbpUrlBuilder GetCurrencyBuilder(NbpTable table, string currency);
    INbpUrlBuilder GetGoldBuilder();
}
