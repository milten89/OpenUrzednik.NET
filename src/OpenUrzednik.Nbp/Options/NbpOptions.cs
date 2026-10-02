namespace OpenUrzednik.Nbp.Options;

/// <summary>
/// Settings for the NBP clients. Every property is optional; with none set, the clients call
/// <see cref="DefaultApiUrl"/> with the <see cref="HttpClient"/>'s own timeout.
/// The clients read the options once, in their constructor; later changes to the instance have no effect.
/// </summary>
public class NbpOptions
{
    /// <summary>
    /// The public NBP API, used when neither <see cref="ApiUrl"/> nor <see cref="HttpClient.BaseAddress"/> is set.
    /// </summary>
    public const string DefaultApiUrl = "https://api.nbp.pl/api/";

    /// <summary>
    /// Base URL of the NBP API, e.g. a proxy or gateway in front of it. Must be an absolute <c>https</c> URL without a query or fragment.
    /// When <see langword="null"/>, the <see cref="HttpClient.BaseAddress"/> is used, then <see cref="DefaultApiUrl"/>.
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
}
