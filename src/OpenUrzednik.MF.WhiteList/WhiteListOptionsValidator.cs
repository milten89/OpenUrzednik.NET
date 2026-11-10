namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// Checks <see cref="WhiteListOptions"/> values. Invalid configuration is a programmer error, so it throws (ADR-0002).
/// </summary>
internal static class WhiteListOptionsValidator
{
    /// <summary>
    /// Parses an absolute <c>https</c> root URL and adds the trailing <c>/</c> that relative request paths need.
    /// A query or fragment is rejected: relative paths would drop it together with the last path segment. A path ending with
    /// <c>api/</c> or <c>api2/</c> is rejected too: this is the host root, and each client adds its own path.
    /// </summary>
    internal static Uri ParseApiUrl(string apiUrl, string paramName)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new ArgumentException("MF White List API url must not be empty.", paramName);

        var url = apiUrl.EndsWith('/') ? apiUrl : apiUrl + "/";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid MF White List API url: {url}", paramName);

        if (uri.Scheme == Uri.UriSchemeHttp)
            throw new ArgumentException($"Invalid MF White List API url scheme: {url}. MF White List API supports only HTTPS.", paramName);

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"Invalid MF White List API url scheme: {url}", paramName);

        if (uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException(
                $"Invalid MF White List API url: {apiUrl}. It must not contain a query or fragment; add query parameters with a DelegatingHandler.",
                paramName);

        // The clients add api/ or api2/ themselves (ADR-0023), so a root that already ends with one would double it.
        var path = uri.AbsolutePath;
        if (path.EndsWith("/api/", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/api2/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Invalid MF White List API url: {apiUrl}. Set the host root (e.g. {WhiteListOptions.DefaultApiUrl}) without api/ or api2/: each client adds its own path.",
                paramName);

        return uri;
    }

    /// <summary>
    /// Accepts the same values as <see cref="HttpClient.Timeout"/>: positive and at most <see cref="int.MaxValue"/> milliseconds, or <see cref="Timeout.InfiniteTimeSpan"/>.
    /// </summary>
    internal static void ValidateTimeout(TimeSpan? timeout, string paramName)
    {
        // HasValue, not `timeout is { } value`, so that mutation testing checks the comparisons (ADR-0020): the pattern's `is not`
        // mutant leaves `value` unassigned where it's used (CS0165), so the mutant doesn't compile, and Stryker discards the other
        // mutants of that code with it.
        var value = timeout.GetValueOrDefault();
        if (timeout.HasValue && value != Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue))
            throw new ArgumentException($"Invalid MF White List API timeout: {value}. It must be positive and at most {int.MaxValue} ms, or Timeout.InfiniteTimeSpan.", paramName);
    }
}
