namespace OpenUrzednik.Nbp.UrlBuilder;

/// <summary>
/// Creates the <see cref="INbpUrlBuilder"/>s the clients use. The default is <see cref="NbpUrlBuilderFactory"/>.
/// </summary>
public interface INbpUrlBuilderFactory
{
    /// <summary>Gets the builder for a rate table.</summary>
    /// <param name="table">Table.</param>
    /// <returns>The builder.</returns>
    INbpUrlBuilder GetTableBuilder(NbpTable table);
    /// <summary>Gets the builder for one currency's rates in a table.</summary>
    /// <param name="table">Table.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <returns>The builder.</returns>
    INbpUrlBuilder GetCurrencyBuilder(NbpTable table, string currency);
    /// <summary>Gets the builder for gold prices.</summary>
    /// <returns>The builder.</returns>
    INbpUrlBuilder GetGoldBuilder();
}
