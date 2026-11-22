using System.Net;

using OpenUrzednik.MF.WhiteList.Common;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The retry filter of ADR-0023 for an app's resilience handler: answers whether an MF White List response is worth sending again.
/// A resilience handler decides from the HTTP status only and can't see the API's codes; this filter adds them. It uses only BCL
/// types, so it fits any resilience library.
/// </summary>
/// <remarks>
/// <para>
/// Retried: a 400 with the code <c>WL-195</c> or <c>WL-196</c> (the list is being updated, the API asks to repeat the query), a 408 and
/// every 5xx but a 500 with the code <c>WL-197</c> (a retry can't fix it and would spend quota). Not retried: a 429 (the daily limit
/// blocks the IP until midnight), a 500 <c>WL-197</c> and anything else (a success, any other 4xx, a 400 with another code or none).
/// </para>
/// <para>
/// For a 400 and a 500 the filter reads the body to find the code: at most 4096 bytes of it, plus one to tell a larger body apart. It replaces the response's content, so
/// the client's error mapping can still read the whole body from the start afterwards. A body over the 4096 bytes (a proxy's error page,
/// not the API's answer) counts as having no code and is not read in full. So does a body that isn't the API's JSON, has no code, or
/// fails while it is read or was consumed before (that body is left as it is, and the client returns the failure as an error).
/// </para>
/// </remarks>
/// <example>
/// Plug it into <c>AddStandardResilienceHandler()</c> (from <c>Microsoft.Extensions.Http.Resilience</c>) next to
/// <c>HttpClientResiliencePredicates.IsTransient</c>, which keeps deciding about exceptions and timeouts:
/// <code>
/// services.AddOpenUrzednikMFWhiteList().AddStandardResilienceHandler(options =>
///     options.Retry.ShouldHandle = args => args.Outcome.Result is { } response
///         ? WhiteListRetry.ShouldRetryAsync(response, args.Context.CancellationToken)
///         : ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome)));
/// </code>
/// </example>
public static class WhiteListRetry
{
    private const int RequestTimeout = 408;
    private const int FirstOtherServerError = 501;
    private const int LastServerError = 599;

    /// <summary>
    /// Answers whether <paramref name="response"/> is worth retrying (see <see cref="WhiteListRetry"/>).
    /// </summary>
    /// <param name="response">The response of an MF White List request. Its body stays readable.</param>
    /// <param name="cancellationToken">Stops the wait for the body of a 400 or 500.</param>
    /// <returns><see langword="true"/> to send the request again, <see langword="false"/> to return the response as it is.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled while the body was read.</exception>
    public static ValueTask<bool> ShouldRetryAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var status = (int)response.StatusCode;
        if (status == (int)HttpStatusCode.BadRequest || status == (int)HttpStatusCode.InternalServerError)
            return new ValueTask<bool>(ShouldRetryByCodeAsync(response, status, cancellationToken));

        return new ValueTask<bool>(status == RequestTimeout || status is >= FirstOtherServerError and <= LastServerError);
    }

    // A 400 is retried for the two "repeat the query" codes, a 500 for any code but the historical-status one.
    private static async Task<bool> ShouldRetryByCodeAsync(HttpResponseMessage response, int status, CancellationToken cancellationToken)
    {
        var code = await WhiteListErrorMapping.ReadCodeAsync(response, cancellationToken).ConfigureAwait(false);
        if (status == (int)HttpStatusCode.BadRequest)
            return code == WhiteListErrorMapping.DatabaseUpdatedCode || code == WhiteListErrorMapping.UpdateInProgressCode;

        return code != WhiteListErrorMapping.HistoricalStatusUnknownCode;
    }
}
