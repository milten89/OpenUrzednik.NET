namespace OpenUrzednik.Nbp.Options;

/// <summary>
/// Settings for the NBP clients. Every property is optional; with none set, the clients call
/// <see cref="DefaultApiUrl"/> with the <see cref="HttpClient"/>'s own timeout.
/// </summary>
public class NbpOptions
{
    /// <summary>
    /// The public NBP API, used when neither <see cref="ApiUrl"/> nor <see cref="HttpClient.BaseAddress"/> is set.
    /// </summary>
    public const string DefaultApiUrl = "https://api.nbp.pl/api/";

    /// <summary>
    /// Base URL of the NBP API, e.g. a proxy or gateway in front of it. Must be an absolute <c>https</c> URL.
    /// When <see langword="null"/>, the <see cref="HttpClient.BaseAddress"/> is used, then <see cref="DefaultApiUrl"/>.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Deadline for one request, including reading the response body. Must be positive or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.
    /// When <see langword="null"/>, the <see cref="HttpClient.Timeout"/> applies (100 seconds unless changed).
    /// The <see cref="HttpClient"/> itself is never changed. When it elapses, the result is a <see cref="OpenUrzednik.Core.Errors.RequestTimeoutError"/>.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
