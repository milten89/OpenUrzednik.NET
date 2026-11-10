using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// Creates the <see cref="RestRequestExecutor"/> the NBP clients send requests with (ADR-0019).
/// </summary>
internal static class NbpConnection
{
    /// <summary>The name that starts the error messages and the options' messages.</summary>
    internal const string DisplayName = "NBP API";

    /// <summary>
    /// The NBP profile: spans are named <c>nbp.http.get</c>, messages start with <c>NBP API</c>. The default error mapping fits NBP,
    /// including its plain-text 400 messages, so there is no override.
    /// </summary>
    // Stryker disable once String : false survivor, Stryker can't activate a static field initializer; an empty name makes RestProviderProfile throw, which fails every client test (checked by hand)
    internal static readonly RestProviderProfile Profile = new("nbp", DisplayName);

    /// <summary>
    /// Creates the executor for a client: base address from <paramref name="options"/>, then
    /// <see cref="HttpClient.BaseAddress"/>, then <see cref="NbpOptions.DefaultApiUrl"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The options or the <see cref="HttpClient.BaseAddress"/> are invalid.</exception>
    internal static RestRequestExecutor Create(HttpClient httpClient, NbpOptions? options, OpenUrzednikTelemetry telemetry, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        var baseAddress = RestOptions.ResolveBaseAddress(options?.ApiUrl, httpClient, NbpOptions.DefaultApiUrl, ParseApiUrl);
        RestOptions.ValidateTimeout(options?.Timeout, DisplayName, nameof(options));

        return new RestRequestExecutor(httpClient, baseAddress, Profile, options?.Timeout, telemetry, timeProvider);
    }

    /// <summary>Checks an NBP base URL: NBP has no rules beyond the shared ones.</summary>
    internal static Uri ParseApiUrl(string apiUrl, string paramName) => RestOptions.ParseBaseUrl(apiUrl, DisplayName, paramName);
}
