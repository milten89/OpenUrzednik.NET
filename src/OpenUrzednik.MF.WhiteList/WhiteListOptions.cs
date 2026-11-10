namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// Settings for the MF White List clients. Every property is optional; with none set, the clients call the production host
/// <see cref="DefaultApiUrl"/> with the <see cref="HttpClient"/>'s own timeout.
/// The clients read the options once, in their constructor; later changes to the instance have no effect.
/// </summary>
/// <remarks>Minor versions may add options; each new one is optional and keeps today's behaviour when unset (ADR-0011).</remarks>
public sealed class WhiteListOptions
{
    /// <summary>
    /// The production host of the MF White List API, used when neither <see cref="ApiUrl"/> nor <see cref="HttpClient.BaseAddress"/> is set.
    /// The search and check clients send requests under its <c>api/</c> path, the status (WL2) client under <c>api2/</c>.
    /// </summary>
    public const string DefaultApiUrl = "https://wykazpodatnikowvat.mf.gov.pl/";

    /// <summary>
    /// The ministry's test host, with its own test data (the lists of NIPs, REGONs and account numbers that return fictitious subjects).
    /// Set <see cref="ApiUrl"/> to it to test an integration without querying the real register.
    /// </summary>
    public const string TestApiUrl = "https://wykazpodatnikowvat-test.mf.gov.pl/";

    /// <summary>
    /// The root URL of the MF White List API host, e.g. <see cref="TestApiUrl"/> or a proxy or gateway in front of the API, without the
    /// <c>api/</c> or <c>api2/</c> path: each client adds its own. Must be an absolute <c>https</c> URL without a query or fragment, whose
    /// path doesn't end with <c>api/</c> or <c>api2/</c> (in any case). When <see langword="null"/>, the <see cref="HttpClient.BaseAddress"/>
    /// is used as the root, under the same rules, then <see cref="DefaultApiUrl"/>.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Deadline for one request, including reading the response body. Must be positive (at most <see cref="int.MaxValue"/> milliseconds)
    /// or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.
    /// When <see langword="null"/>, the <see cref="HttpClient.Timeout"/> applies (100 seconds unless changed).
    /// The <see cref="HttpClient"/> itself is never changed, and its <see cref="HttpClient.Timeout"/> still limits the wait for the response headers:
    /// this option can shorten that limit but not extend it, and <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> only removes this deadline.
    /// When a deadline elapses, the result is a <see cref="OpenUrzednik.Core.Errors.RequestTimeoutError"/>.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Checks the values the clients would reject in their constructor, e.g. to fail at startup instead of on the first client.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="ApiUrl"/> isn't an absolute <c>https</c> URL without a query or fragment, its path
    /// ends with <c>api/</c> or <c>api2/</c>,
    /// or <see cref="Timeout"/> isn't positive (at most <see cref="int.MaxValue"/> milliseconds) or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.</exception>
    public void Validate()
    {
        // A local and `is not null`, not a pattern variable, so that mutation testing checks the call: see WhiteListOptionsValidator (ADR-0020).
        var apiUrl = ApiUrl;
        if (apiUrl is not null)
            WhiteListOptionsValidator.ParseApiUrl(apiUrl, nameof(ApiUrl));

        WhiteListOptionsValidator.ValidateTimeout(Timeout, nameof(Timeout));
    }
}
