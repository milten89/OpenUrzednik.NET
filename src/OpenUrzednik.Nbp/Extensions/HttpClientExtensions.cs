using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Options;

namespace OpenUrzednik.Nbp.Extensions;

public static class HttpClientExtensions
{
    public static HttpClient ConfigureForNbpApi(this HttpClient httpClient, NbpOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        
        if (string.IsNullOrWhiteSpace(options.ApiUrl))
            throw new ArgumentException("API url must be provided", nameof(options));
        
        var url = options.ApiUrl[^1] == '/' ? options.ApiUrl : $"{options.ApiUrl}/";
        
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"Invalid API url: {url}", nameof(options));

        if (options.Timeout != Timeout.InfiniteTimeSpan &&
            options.Timeout <= TimeSpan.Zero)
            throw new ArgumentException($"Invalid timeout value: {options.Timeout}", nameof(options));
        
        httpClient.BaseAddress = uri;
        httpClient.Timeout = options.Timeout;

        return httpClient;
    }

    internal static async Task<OpenUrzednikResult<TDto>> GetNbpAsync<TDto>(this HttpClient httpClient, string relativePath, JsonTypeInfo<TDto> typeInfo, TimeProvider? timeProvider = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(typeInfo);
        
        cancellationToken.ThrowIfCancellationRequested();
        
        var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                       .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return OpenUrzednikResult.Failure(new NotFoundError($"Resurce at {response.RequestMessage?.RequestUri?.ToString() ?? relativePath} was not found."));

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return OpenUrzednikResult.Failure(new RateLimitExceededError("To many requests.", GetDelay(response, timeProvider)));

        if (!response.IsSuccessStatusCode)
            return OpenUrzednikResult.Failure(new UnknownError($"NBP API return unknown status: {(int)response.StatusCode}."));

        try
        {
            var dto = await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);

            return dto is not null
                ? OpenUrzednikResult.Success(dto)
                : OpenUrzednikResult.Failure(new UnknownError($"NBP API return empty response for {relativePath}."));
        }
        catch (JsonException ex)
        {
            return OpenUrzednikResult.Failure(new SerializationError($"Error when serializing response from {relativePath}", ex));
        }
    }

    private static TimeSpan? GetDelay(HttpResponseMessage response, TimeProvider? timeProvider)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
            return null;

        if (retryAfter.Delta.HasValue)
            return retryAfter.Delta.Value;

        if (retryAfter.Date.HasValue)
        {
            var responseDate = response.Headers.Date ?? timeProvider?.GetUtcNow() ?? TimeProvider.System.GetUtcNow();
            return retryAfter.Date.Value - responseDate;
        }

        return null;
    }
}
