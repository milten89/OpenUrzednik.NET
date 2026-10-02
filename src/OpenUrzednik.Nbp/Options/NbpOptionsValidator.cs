namespace OpenUrzednik.Nbp.Options;

/// <summary>
/// Checks <see cref="NbpOptions"/> values. Invalid configuration is a programmer error, so it throws (ADR-0002).
/// </summary>
internal static class NbpOptionsValidator
{
    /// <summary>
    /// Parses an absolute <c>https</c> base URL and adds the trailing <c>/</c> that relative request paths need.
    /// </summary>
    internal static Uri ParseApiUrl(string apiUrl, string paramName)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentException("NBP API url must not be empty.", paramName);

        var url = apiUrl.EndsWith("/", StringComparison.Ordinal) ? apiUrl : apiUrl + "/";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid NBP API url: {url}", paramName);

        if (uri.Scheme == Uri.UriSchemeHttp)
            throw new ArgumentException($"Invalid NBP API url scheme: {url}. NBP API no longer supports HTTP.", paramName);

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"Invalid NBP API url scheme: {url}", paramName);

        return uri;
    }

    internal static void ValidateTimeout(TimeSpan? timeout, string paramName)
    {
        if (timeout is { } value && value != Timeout.InfiniteTimeSpan && value <= TimeSpan.Zero)
            throw new ArgumentException($"Invalid NBP API timeout: {value}. It must be positive or Timeout.InfiniteTimeSpan.", paramName);
    }
}
