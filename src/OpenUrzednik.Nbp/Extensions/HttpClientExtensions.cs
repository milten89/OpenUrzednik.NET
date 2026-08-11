using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Telemetry;

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

    internal static async Task<OpenUrzednikResult<TDto>> GetNbpAsync<TDto>(this HttpClient httpClient, string relativePath, JsonTypeInfo<TDto> typeInfo, NbpTelemetryProvider telemetryProvider, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(telemetryProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        cancellationToken.ThrowIfCancellationRequested();

        using var traceSpan = telemetryProvider.Tracer.StartSpan("nbp.http.get");
        traceSpan.SetTag("http.path", relativePath);

        var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                       .ConfigureAwait(false);
        traceSpan.SetTag("http.status_code", (int)response.StatusCode);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            if (telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP resource not found: {path}.", "path", relativePath);
            return OpenUrzednikResult.Failure(new NotFoundError(
                $"Resource at {response.RequestMessage?.RequestUri?.ToString() ?? relativePath} was not found."));
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var delay = GetDelay(response, timeProvider);
            if (telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Warning))
                if (delay.HasValue)
                    telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP rate limit hit, retry after {delay}.", "delay", delay);
                else
                    telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP rate limit hit.");
            traceSpan.SetStatus(SpanStatus.Error, "Rate limited");
            return OpenUrzednikResult.Failure(new RateLimitExceededError("Too many requests.", delay));
        }

        if (!response.IsSuccessStatusCode)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP API returned unexpected status {status}", "status", (int)response.StatusCode);
            traceSpan.SetStatus(SpanStatus.Error, "Unexpected status");
            return OpenUrzednikResult.Failure(
                new UnknownError($"NBP API return unknown status: {(int)response.StatusCode}."));
        }

        try
        {
            var dto = await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);

            return dto is not null
                ? OpenUrzednikResult.Success(dto)
                : OpenUrzednikResult.Failure(new UnknownError($"NBP API return empty response for {relativePath}."));
        }
        catch (JsonException ex)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to deserialize NBP response from {path}", "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(SpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure(new SerializationError($"Error when deserializing response from {relativePath}", ex));
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
