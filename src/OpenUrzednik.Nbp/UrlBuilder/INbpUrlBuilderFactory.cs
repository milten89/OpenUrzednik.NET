using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.UrlBuilder;

public interface INbpUrlBuilderFactory
{
    OpenUrzednikResult<INbpUrlBuilder> GetTableBuilder(NbpTable table);
    OpenUrzednikResult<INbpUrlBuilder> GetCurrencyBuilder(NbpTable table, string currency);
    OpenUrzednikResult<INbpUrlBuilder> GetGoldBuilder();
}
