using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// Creates the <see cref="RestRequestExecutor"/> the NBP clients send requests with (ADR-0019).
/// </summary>
internal static class NbpConnection
{
    /// <summary>
    /// The NBP profile: spans are named <c>nbp.http.get</c>, messages start with <c>NBP API</c>. The default error mapping fits NBP,
    /// including its plain-text 400 messages, so there is no override.
    /// </summary>
    // Stryker disable once String : false survivor, Stryker can't activate a static field initializer; an empty name or display name makes RestProviderProfile throw, which fails every client test (checked by hand)
    internal static readonly RestProviderProfile Profile = new("nbp", "NBP API");

    /// <summary>
    /// Creates the executor for a client: base address from <paramref name="options"/>, then
    /// <see cref="HttpClient.BaseAddress"/>, then <see cref="NbpOptions.DefaultApiUrl"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The options or the <see cref="HttpClient.BaseAddress"/> are invalid.</exception>
    internal static RestRequestExecutor Create(HttpClient httpClient, NbpOptions? options, OpenUrzednikTelemetry telemetry, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        // Locals and `is not null`, not pattern variables, so that mutation testing checks this method: see NbpOptionsValidator (ADR-0018).
        var apiUrl = options?.ApiUrl;
        var clientBaseAddress = httpClient.BaseAddress;
        Uri baseAddress;
        if (apiUrl is not null)
            baseAddress = NbpOptionsValidator.ParseApiUrl(apiUrl, nameof(options));
        else if (clientBaseAddress is not null)
            baseAddress = NbpOptionsValidator.ParseApiUrl(clientBaseAddress.OriginalString, nameof(httpClient));
        else
            baseAddress = new Uri(NbpOptions.DefaultApiUrl);

        NbpOptionsValidator.ValidateTimeout(options?.Timeout, nameof(options));

        return new RestRequestExecutor(httpClient, baseAddress, Profile, options?.Timeout, telemetry, timeProvider);
    }
}
