namespace OpenUrzednik.Nbp.Options;

/// <summary>
/// Checks <see cref="NbpOptions"/> values. Invalid configuration is a programmer error, so it throws (ADR-0002).
/// </summary>
internal static class NbpOptionsValidator
{
    /// <summary>
    /// Parses an absolute <c>https</c> base URL and adds the trailing <c>/</c> that relative request paths need.
    /// A query or fragment is rejected: relative paths would drop it together with the last path segment.
    /// </summary>
    internal static Uri ParseApiUrl(string apiUrl, string paramName)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentException("NBP API url must not be empty.", paramName);

        var url = apiUrl.EndsWith('/') ? apiUrl : apiUrl + "/";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid NBP API url: {url}", paramName);

        if (uri.Scheme == Uri.UriSchemeHttp)
            throw new ArgumentException($"Invalid NBP API url scheme: {url}. NBP API no longer supports HTTP.", paramName);

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"Invalid NBP API url scheme: {url}", paramName);

        if (uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException(
                $"Invalid NBP API url: {apiUrl}. It must not contain a query or fragment; add query parameters with a DelegatingHandler or a custom INbpUrlBuilderFactory.",
                paramName);

        return uri;
    }

    /// <summary>
    /// Accepts the same values as <see cref="HttpClient.Timeout"/>: positive and at most <see cref="int.MaxValue"/> milliseconds, or <see cref="Timeout.InfiniteTimeSpan"/>.
    /// </summary>
    internal static void ValidateTimeout(TimeSpan? timeout, string paramName)
    {
        if (timeout is { } value && value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue))
            throw new ArgumentException($"Invalid NBP API timeout: {value}. It must be positive and at most {int.MaxValue} ms, or Timeout.InfiniteTimeSpan.", paramName);
    }
}
