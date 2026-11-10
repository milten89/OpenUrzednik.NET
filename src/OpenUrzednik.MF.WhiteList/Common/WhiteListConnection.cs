using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// Creates the <see cref="RestRequestExecutor"/>s the MF White List clients send requests with (ADR-0019, ADR-0023): the search and check
/// client under the host's <c>api/</c> path, the status (WL2) client under <c>api2/</c>.
/// </summary>
internal static class WhiteListConnection
{
    /// <summary>The path of the search and check API, relative to the host root.</summary>
    internal const string SearchApiPath = "api/";

    /// <summary>The path of the status API (WL2), relative to the host root.</summary>
    internal const string StatusApiPath = "api2/";

    /// <summary>The short name in span names (<c>mf_white_list.http.get</c>) and the <c>{provider}</c> log property (ADR-0016).</summary>
    internal const string ProviderName = "mf_white_list";

    /// <summary>The name that starts the error messages.</summary>
    internal const string DisplayName = "MF White List API";

    /// <summary>
    /// Creates the executor of the search and check client: base address the host root plus <see cref="SearchApiPath"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The options or the <see cref="HttpClient.BaseAddress"/> are invalid.</exception>
    internal static RestRequestExecutor CreateSearch(HttpClient httpClient, WhiteListOptions? options, OpenUrzednikTelemetry telemetry, TimeProvider timeProvider)
        => Create(httpClient, options, telemetry, timeProvider, SearchApiPath);

    /// <summary>
    /// Creates the executor of the status (WL2) client: base address the host root plus <see cref="StatusApiPath"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The options or the <see cref="HttpClient.BaseAddress"/> are invalid.</exception>
    internal static RestRequestExecutor CreateStatus(HttpClient httpClient, WhiteListOptions? options, OpenUrzednikTelemetry telemetry, TimeProvider timeProvider)
        => Create(httpClient, options, telemetry, timeProvider, StatusApiPath);

    // The host root from the options, then HttpClient.BaseAddress, then WhiteListOptions.DefaultApiUrl; the profile's error mapping uses
    // the client's clock.
    private static RestRequestExecutor Create(HttpClient httpClient, WhiteListOptions? options, OpenUrzednikTelemetry telemetry, TimeProvider timeProvider, string apiPath)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(timeProvider);

        // Locals and `is not null`, not pattern variables, so that mutation testing checks this method: see WhiteListOptionsValidator (ADR-0020).
        var apiUrl = options?.ApiUrl;
        var clientBaseAddress = httpClient.BaseAddress;
        Uri root;
        if (apiUrl is not null)
            root = WhiteListOptionsValidator.ParseApiUrl(apiUrl, nameof(options));
        else if (clientBaseAddress is not null)
            root = WhiteListOptionsValidator.ParseApiUrl(clientBaseAddress.OriginalString, nameof(httpClient));
        else
            root = new Uri(WhiteListOptions.DefaultApiUrl);

        WhiteListOptionsValidator.ValidateTimeout(options?.Timeout, nameof(options));

        var profile = new RestProviderProfile(ProviderName, DisplayName, context => WhiteListErrorMapping.MapAsync(context, timeProvider));
        return new RestRequestExecutor(httpClient, new Uri(root, apiPath), profile, options?.Timeout, telemetry, timeProvider);
    }
}
