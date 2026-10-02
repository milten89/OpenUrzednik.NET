using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Options;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// Creates the <see cref="RestRequestExecutor"/> the NBP clients send requests with (ADR-0006).
/// </summary>
internal static class NbpConnection
{
    /// <summary>
    /// The NBP profile: spans are named <c>nbp.http.get</c>, messages start with <c>NBP API</c>. The default error mapping fits NBP,
    /// including its plain-text 400 messages, so there is no override.
    /// </summary>
    internal static readonly RestProviderProfile Profile = new("nbp", "NBP API");

    /// <summary>
    /// Creates the executor for a client: base address from <paramref name="options"/>, then
    /// <see cref="HttpClient.BaseAddress"/>, then <see cref="NbpOptions.DefaultApiUrl"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The options or the <see cref="HttpClient.BaseAddress"/> are invalid.</exception>
    internal static RestRequestExecutor Create(HttpClient httpClient, NbpOptions? options, OpenUrzednikTelemetry telemetry, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        Uri baseAddress;
        if (options?.ApiUrl is { } apiUrl)
            baseAddress = NbpOptionsValidator.ParseApiUrl(apiUrl, nameof(options));
        else if (httpClient.BaseAddress is { } clientBaseAddress)
            baseAddress = NbpOptionsValidator.ParseApiUrl(clientBaseAddress.OriginalString, nameof(httpClient));
        else
            baseAddress = new Uri(NbpOptions.DefaultApiUrl);

        NbpOptionsValidator.ValidateTimeout(options?.Timeout, nameof(options));

        return new RestRequestExecutor(httpClient, baseAddress, Profile, options?.Timeout, telemetry, timeProvider);
    }
}
