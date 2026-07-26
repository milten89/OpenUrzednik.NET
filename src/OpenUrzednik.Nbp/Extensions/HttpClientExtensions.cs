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
        var apiUrl = options.ApiUrl.EndsWith('/') ? options.ApiUrl : options.ApiUrl + '/';
        httpClient.BaseAddress = new Uri(apiUrl);
        httpClient.Timeout = options.Timeout;

        return httpClient;
    }

    internal static async Task<OpenUrzednikResult<TDto>> GetNbpAsync<TDto>(this HttpClient httpClient, string relativePath, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                       .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return OpenUrzednikResult.Failure(new NotFoundError($"Resurce at {response.RequestMessage?.RequestUri?.ToString() ?? relativePath} was not found."));

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return OpenUrzednikResult.Failure(new RateLimitExceededError("To many requests."));

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
}
