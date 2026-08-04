namespace OpenUrzednik.Nbp.UrlBuilder;

public class DefaultNbpUrlBuilder : INbpUrlBuilder
{
    private readonly string _baseUrl;

    public DefaultNbpUrlBuilder(string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);

        _baseUrl = baseUrl.EndsWith('/') ? baseUrl[..^1] : baseUrl;
    }

    public string Latest()
        => _baseUrl;

    public string ForTopCount(int topCount)
        => $"{_baseUrl}/last/{topCount}";

    public string Today() 
        => $"{_baseUrl}/today";

    public string ForDate(DateOnly date)
        => $"{_baseUrl}/{date:yyyy-MM-dd}";

    public string ForDateRange(DateOnly from, DateOnly to)
        => $"{_baseUrl}/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}";
}
