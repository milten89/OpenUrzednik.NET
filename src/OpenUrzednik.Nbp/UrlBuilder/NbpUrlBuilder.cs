using OpenUrzednik.Nbp.Extensions;

namespace OpenUrzednik.Nbp.UrlBuilder;

/// <summary>
/// Default <see cref="INbpUrlBuilder"/>: appends the NBP API path segments (<c>last/{n}</c>, <c>today</c>, <c>yyyy-MM-dd</c>) to a resource path.
/// </summary>
public class NbpUrlBuilder : INbpUrlBuilder
{
    private readonly string _baseUrl;

    /// <summary>Initializes a builder for one resource.</summary>
    /// <param name="baseUrl">Path of the resource, e.g. <c>exchangerates/tables/a</c>; a trailing slash is removed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="baseUrl"/> is <see langword="null"/>.</exception>
    public NbpUrlBuilder(string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);

        _baseUrl = baseUrl.EndsWith('/') ? baseUrl.Substring(0, baseUrl.Length - 1) : baseUrl;
    }

    /// <inheritdoc/>
    public string Latest()
        => _baseUrl;

    /// <inheritdoc/>
    public string ForTopCount(int topCount)
        => $"{_baseUrl}/last/{topCount.ToInvariantString()}";

    /// <inheritdoc/>
    public string Today()
        => $"{_baseUrl}/today";

    /// <inheritdoc/>
    public string ForDate(DateOnly date)
        => $"{_baseUrl}/{date.ToIso8601String()}";

    /// <inheritdoc/>
    public string ForDateRange(DateOnly from, DateOnly to)
        => $"{_baseUrl}/{from.ToIso8601String()}/{to.ToIso8601String()}";
}
