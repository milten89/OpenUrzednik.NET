using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.UrlBuilder;

public class DefaultNbpUlrBuilder : INbpUrlBuilder
{
    private readonly string _baseUrl;

    public DefaultNbpUlrBuilder(string baseUrl)
        => _baseUrl = baseUrl.EndsWith('/') ? baseUrl[..^1] : baseUrl;

    public OpenUrzednikResult<string> Latest()
        => OpenUrzednikResult.Success(_baseUrl);

    public OpenUrzednikResult<string> ForTopCount(int topCount)
    {
        var validation = new ValueValidator<int>(nameof(topCount), topCount).EnsureProperTopCount();
        return validation.IsValid 
                   ? OpenUrzednikResult.Success($"{_baseUrl}/last/{topCount}") 
                   : validation.ToResult();
    }

    public OpenUrzednikResult<string> Today() 
        => OpenUrzednikResult.Success($"{_baseUrl}/today");

    public OpenUrzednikResult<string> ForDate(DateOnly date)
    {
        var validation = new ValueValidator<DateOnly>(nameof(date), date).EnsureGreaterThanMinGoldDate();
        return validation.IsValid 
                   ? OpenUrzednikResult.Success($"{_baseUrl}/{date:yyyy-MM-dd}") 
                   : validation.ToResult();
    }

    public OpenUrzednikResult<string> ForDateRange(DateOnly from, DateOnly to)
    {
        var toValidation = new ValueValidator<DateOnly>(nameof(to), to).EnsureGreaterThanMinGoldDate();
        var rangeValidation = new ValueValidator<(DateOnly, DateOnly)>((from, to)).EnsureValidRange()
                                                                                  .EnsureRangeSize();
        return toValidation.IsValid && rangeValidation.IsValid
                   ? OpenUrzednikResult.Success($"{_baseUrl}/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}")
                   : ValidatorCombine.ToResult(toValidation, rangeValidation);
    }
}
