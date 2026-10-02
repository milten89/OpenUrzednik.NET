using OpenUrzednik.Nbp.Extensions;

namespace OpenUrzednik.Nbp.UrlBuilder;

public class NbpUrlBuilder : INbpUrlBuilder
{
    private readonly string _baseUrl;

    public NbpUrlBuilder(string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);

        _baseUrl = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl.Substring(0, baseUrl.Length - 1) : baseUrl;
    }

    public string Latest()
        => _baseUrl;

    public string ForTopCount(int topCount)
        => $"{_baseUrl}/last/{topCount.ToInvariantString()}";

    public string Today()
        => $"{_baseUrl}/today";

    public string ForDate(DateOnly date)
        => $"{_baseUrl}/{date.ToIso8601String()}";

    public string ForDateRange(DateOnly from, DateOnly to)
        => $"{_baseUrl}/{from.ToIso8601String()}/{to.ToIso8601String()}";
}
