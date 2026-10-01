using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
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
    private const int MaxServerMessageLength = 500;

    public static HttpClient ConfigureForNbpApi(this HttpClient httpClient, NbpOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiUrl))
            throw new ArgumentException("API url must be provided", nameof(options));

        var url = options.ApiUrl[^1] == '/' ? options.ApiUrl : $"{options.ApiUrl}/";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid API url: {url}", nameof(options));

        if (uri.Scheme == Uri.UriSchemeHttp)
            throw new ArgumentException($"Invalid API url scheme: {url}. NBP API no longer supports HTTP", nameof(options));

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"Invalid API url scheme: {url}", nameof(options));

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

        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

        using var response = await SendAsync(httpClient, request, traceSpan, cancellationToken).ConfigureAwait(false);
        traceSpan.SetTag("http.status_code", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            return await MapErrorResponseAsync(response, relativePath, telemetryProvider, traceSpan, timeProvider, cancellationToken).ConfigureAwait(false);

        try
        {
            var dto = await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);

            return dto is not null
                ? OpenUrzednikResult.Success(dto)
                : OpenUrzednikResult.Failure(new UnknownError($"NBP API return empty response for {relativePath}."));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to deserialize NBP response from {path}", "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure(new SerializationError($"Error when deserializing response from {relativePath}", ex));
        }
        catch (Exception ex)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, ex, "Unexpected error while reading NBP response from {path}", "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected error");
            throw;
        }
    }

    private static async Task<OpenUrzednikResult> MapErrorResponseAsync(HttpResponseMessage response, string relativePath, NbpTelemetryProvider telemetryProvider, IOpenUrzednikSpan traceSpan, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var path = response.RequestMessage?.RequestUri?.ToString() ?? relativePath;

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                {
                    if (telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                        telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP resource not found: {path}.", "path", path);
                    return OpenUrzednikResult.Failure(new NotFoundError(
                        $"Resource at {path} was not found.", statusCode));
                }
            case HttpStatusCode.TooManyRequests:
                {
                    var delay = GetDelay(response, timeProvider);
                    if (telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Warning))
                    {
                        if (delay.HasValue)
                            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP rate limit hit, retry after {delay}.", "delay", delay);
                        else
                            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP rate limit hit.");
                    }
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Rate limited");
                    return OpenUrzednikResult.Failure(new RateLimitExceededError("Too many requests.", delay, statusCode));
                }
            case HttpStatusCode.BadRequest:
                {
                    var serverMessage = await ReadServerMessageAsync(response, telemetryProvider, cancellationToken).ConfigureAwait(false);
                    telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP API rejected the request to {path}", "path", path);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Bad request");
                    return OpenUrzednikResult.Failure(new BadRequestError(
                        serverMessage is null
                            ? $"NBP API rejected the request to {path}."
                            : $"NBP API rejected the request to {path}: {serverMessage}",
                        statusCode));
                }
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                {
                    telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP API denied access to {path} with status {status}", "path", path, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unauthorized");
                    return OpenUrzednikResult.Failure(new UnauthorizedError(
                        $"NBP API denied access to {path} with status {statusCode}.", statusCode));
                }
            case >= HttpStatusCode.InternalServerError:
                {
                    telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP API is unavailable, status {status}", "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Service unavailable");
                    return OpenUrzednikResult.Failure(new ServiceUnavailableError(
                        $"NBP API is unavailable, status {statusCode}.", statusCode));
                }
            default:
                {
                    telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, null, "NBP API returned unexpected status {status}", "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected status");
                    return OpenUrzednikResult.Failure(
                        new UnknownError($"NBP API return unknown status: {statusCode}.", statusCode));
                }
        }
    }

    /// <summary>
    /// Reads the plain-text message NBP returns with error responses (e.g. <c>400 BadRequest - Błędny zakres dat</c>).
    /// At most <see cref="MaxServerMessageLength"/> characters are read, so a large body (e.g. an HTML page from a proxy) is not buffered.
    /// Returns <see langword="null"/> when there is no body or it can't be read, because the message is only extra context.
    /// </summary>
    private static async Task<string?> ReadServerMessageAsync(HttpResponseMessage response, NbpTelemetryProvider telemetryProvider, CancellationToken cancellationToken)
    {
        var buffer = new char[MaxServerMessageLength];
        var length = 0;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            int read;
            while (length < buffer.Length &&
                   (read = await reader.ReadAsync(buffer.AsMemory(length), cancellationToken).ConfigureAwait(false)) > 0)
                length += read;
        }
        catch (HttpRequestException ex)
        {
            LogUnreadableBody(telemetryProvider, ex);
            return null;
        }
        catch (IOException ex)
        {
            LogUnreadableBody(telemetryProvider, ex);
            return null;
        }

        var message = new string(buffer, 0, length).Trim();
        return message.Length == 0 ? null : message;
    }

    private static void LogUnreadableBody(NbpTelemetryProvider telemetryProvider, Exception exception)
    {
        if (telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, exception, "Could not read NBP error response body.");
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, HttpRequestMessage request, IOpenUrzednikSpan traceSpan, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            traceSpan.RecordException(ex);
            throw;
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
