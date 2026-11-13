namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// The options checks every REST provider shares: its base URL (<c>ApiUrl</c>), its <c>Timeout</c>, and which base address a client
/// uses. Invalid configuration is a programmer error, so the checks throw (ADR-0002). Messages start with the provider's display name,
/// e.g. <c>NBP API</c>; rules of one provider only (e.g. a path the root must not end with) stay in the provider.
/// </summary>
internal static class RestOptions
{
    /// <summary>
    /// Parses an absolute <c>https</c> base URL and adds the trailing <c>/</c> that relative request paths need.
    /// A query or fragment is rejected: relative paths would drop it together with the last path segment.
    /// </summary>
    /// <param name="url">The URL from the options or the <see cref="HttpClient.BaseAddress"/>.</param>
    /// <param name="displayName">The provider's name that starts the messages, e.g. <c>NBP API</c>.</param>
    /// <param name="paramName">The parameter or property the exception names.</param>
    /// <exception cref="ArgumentException"><paramref name="url"/> is empty, not absolute, not <c>https</c>, or has a query or fragment.</exception>
    internal static Uri ParseBaseUrl(string url, string displayName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException($"{displayName} url must not be empty.", paramName);

        var withSlash = url.EndsWith('/') ? url : url + "/";

        if (!Uri.TryCreate(withSlash, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid {displayName} url: {withSlash}", paramName);

        if (uri.Scheme == Uri.UriSchemeHttp)
            throw new ArgumentException($"Invalid {displayName} url scheme: {withSlash}. {displayName} supports only HTTPS.", paramName);

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"Invalid {displayName} url scheme: {withSlash}", paramName);

        if (uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException(
                $"Invalid {displayName} url: {url}. It must not contain a query or fragment; add query parameters with a DelegatingHandler.",
                paramName);

        return uri;
    }

    /// <summary>
    /// Accepts the same values as <see cref="HttpClient.Timeout"/>: positive and at most <see cref="int.MaxValue"/> milliseconds, or
    /// <see cref="Timeout.InfiniteTimeSpan"/>. <see langword="null"/> (the <see cref="HttpClient.Timeout"/> applies) is valid too.
    /// The one copy of the range: <see cref="ValidateTimeout"/> and <see cref="RestRequestExecutor"/>'s constructor use it, each with
    /// its own exception.
    /// </summary>
    /// <param name="timeout">The timeout to check.</param>
    /// <returns><see langword="true"/> when <paramref name="timeout"/> is in range or <see langword="null"/>.</returns>
    internal static bool IsValidTimeout(TimeSpan? timeout)
    {
        // HasValue, not a pattern variable (`timeout is not { } value || …`), so that mutation testing checks the comparisons
        // (ADR-0020): negating the pattern leaves `value` unassigned where it's used (CS0165), so the mutant doesn't compile, and
        // Stryker discards the other mutants of that code with it.
        var value = timeout.GetValueOrDefault();
        return !timeout.HasValue || value == Timeout.InfiniteTimeSpan || (value > TimeSpan.Zero && value.TotalMilliseconds <= int.MaxValue);
    }

    /// <summary>
    /// Checks an options timeout with <see cref="IsValidTimeout"/>; the message starts with the provider's display name.
    /// </summary>
    /// <param name="timeout">The timeout from the options.</param>
    /// <param name="displayName">The provider's name that starts the message, e.g. <c>NBP API</c>.</param>
    /// <param name="paramName">The parameter or property the exception names.</param>
    /// <exception cref="ArgumentException"><paramref name="timeout"/> is out of range.</exception>
    internal static void ValidateTimeout(TimeSpan? timeout, string displayName, string paramName)
    {
        if (!IsValidTimeout(timeout))
            throw new ArgumentException($"Invalid {displayName} timeout: {timeout}. It must be positive and at most {int.MaxValue} ms, or Timeout.InfiniteTimeSpan.", paramName);
    }

    /// <summary>
    /// Picks a client's base address: the options' URL, then the <see cref="HttpClient.BaseAddress"/>, then the provider's default.
    /// The first two are checked with <paramref name="parse"/>, which names the parameter the value came from: <c>options</c> or
    /// <c>httpClient</c>, the parameter names of the clients' constructors (ADR-0007). The default isn't checked: it's the provider's constant.
    /// </summary>
    /// <param name="optionsUrl">The options' <c>ApiUrl</c>, or <see langword="null"/> when there are no options or it isn't set.</param>
    /// <param name="httpClient">The client's <see cref="HttpClient"/>; it isn't changed.</param>
    /// <param name="defaultUrl">The provider's default base URL, absolute and ending with <c>/</c>.</param>
    /// <param name="parse">The provider's check of a URL and the parameter name for its exception, e.g. a call to
    /// <see cref="ParseBaseUrl(string, string, string)"/> with the provider's display name, followed by the provider's own rules.</param>
    /// <exception cref="ArgumentException"><paramref name="parse"/> rejected the options' URL or the <see cref="HttpClient.BaseAddress"/>.</exception>
    internal static Uri ResolveBaseAddress(string? optionsUrl, HttpClient httpClient, string defaultUrl, Func<string, string, Uri> parse)
    {
        // Locals and `is not null`, not pattern variables, so that mutation testing checks this method: a pattern variable's `is` mutant
        // leaves it unassigned where it's used (CS0165), and Stryker discards the other mutants of that code with it (ADR-0020).
        var clientBaseAddress = httpClient.BaseAddress;
        if (optionsUrl is not null)
            return parse(optionsUrl, "options");

        if (clientBaseAddress is not null)
            return parse(clientBaseAddress.OriginalString, "httpClient");

        return new Uri(defaultUrl);
    }
}
