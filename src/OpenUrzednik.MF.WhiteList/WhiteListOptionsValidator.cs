using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// Checks <see cref="WhiteListOptions.ApiUrl"/> (or the <see cref="HttpClient.BaseAddress"/>) against the MF White List rule on top of the
/// shared ones in <see cref="RestOptions"/>. Invalid configuration is a programmer error, so it throws (ADR-0002).
/// </summary>
internal static class WhiteListOptionsValidator
{
    /// <summary>
    /// Parses an absolute <c>https</c> root URL as <see cref="RestOptions.ParseBaseUrl(string, string, string)"/> does, then rejects a
    /// path ending with <c>api/</c> or <c>api2/</c>: this is the host root, and each client adds its own path.
    /// </summary>
    internal static Uri ParseApiUrl(string apiUrl, string paramName)
    {
        var uri = RestOptions.ParseBaseUrl(apiUrl, WhiteListConnection.DisplayName, paramName);

        // The clients add api/ or api2/ themselves (ADR-0023), so a root that already ends with one would double it.
        var path = uri.AbsolutePath;
        if (path.EndsWith("/api/", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/api2/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Invalid MF White List API url: {apiUrl}. Set the host root (e.g. {WhiteListOptions.DefaultApiUrl}) without api/ or api2/: each client adds its own path.",
                paramName);

        return uri;
    }
}
