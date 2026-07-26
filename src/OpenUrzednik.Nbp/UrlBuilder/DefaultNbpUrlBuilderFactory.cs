using System.Collections.Concurrent;
using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.UrlBuilder;

public class DefaultNbpUrlBuilderFactory : INbpUrlBuilderFactory
{
    private static readonly ConcurrentDictionary<string, INbpUrlBuilder> Cache = new();

    public OpenUrzednikResult<INbpUrlBuilder> GetTableBuilder(NbpTable table)
    {
        var tableValidator = new ValueValidator<NbpTable>(nameof(table), table).EnsureDefinedEnum();

        return tableValidator.IsValid
            ? OpenUrzednikResult.Success(GetOrAdd($"exchangerates/tables/{table}"))
            : (OpenUrzednikResult<INbpUrlBuilder>)tableValidator.ToResult();
    }

    public OpenUrzednikResult<INbpUrlBuilder> GetCurrencyBuilder(NbpTable table, string currency)
    {
        var tableValidator = new ValueValidator<NbpTable>(nameof(table), table).EnsureDefinedEnum();
        var currencyValidator = new ValueValidator<string>(nameof(currency), currency).EnsureIso4217Code();

        return tableValidator.IsValid && currencyValidator.IsValid
            ? OpenUrzednikResult.Success(GetOrAdd($"exchangerates/rates/{table}/{Uri.EscapeDataString(currency)}"))
            : (OpenUrzednikResult<INbpUrlBuilder>)ValidatorCombine.ToResult(tableValidator, currencyValidator);
    }

    public OpenUrzednikResult<INbpUrlBuilder> GetGoldBuilder() 
        => OpenUrzednikResult.Success(GetOrAdd("cenyzlota"));    

    private static INbpUrlBuilder GetOrAdd(string relativeUrl) 
        => Cache.GetOrAdd(relativeUrl, url => new DefaultNbpUlrBuilder(url));
}