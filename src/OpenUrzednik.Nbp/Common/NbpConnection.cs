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

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// Sends requests to the NBP API and turns every response into an <see cref="OpenUrzednikResult{TValue}"/> (ADR-0002).
/// Requests use absolute URIs built from the resolved base address, so the caller's <see cref="HttpClient"/> is never changed.
/// </summary>
internal sealed class NbpConnection
{
    private const int MaxServerMessageLength = 500;

    private readonly HttpClient _httpClient;
    private readonly Uri _baseAddress;
    private readonly TimeSpan? _timeout;
    private readonly NbpTelemetryProvider _telemetryProvider;
    private readonly TimeProvider _timeProvider;

    internal NbpConnection(HttpClient httpClient, Uri baseAddress, TimeSpan? timeout, NbpTelemetryProvider telemetryProvider, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(telemetryProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _baseAddress = baseAddress;
        _timeout = timeout;
        _telemetryProvider = telemetryProvider;
        _timeProvider = timeProvider;
    }

    internal Uri BaseAddress => _baseAddress;

    /// <summary>
    /// The request deadline: <see cref="NbpOptions.Timeout"/> when set, otherwise <see cref="HttpClient.Timeout"/>.
    /// It also covers reading the body, which <see cref="HttpClient.Timeout"/> doesn't with <see cref="HttpCompletionOption.ResponseHeadersRead"/>.
    /// <see langword="null"/> means no deadline.
    /// </summary>
    internal TimeSpan? Timeout => Finite(_timeout ?? _httpClient.Timeout);

    /// <summary>
    /// The deadline until the headers arrive. <see cref="HttpClient.Timeout"/> still applies inside <see cref="HttpClient.SendAsync(HttpRequestMessage, HttpCompletionOption, CancellationToken)"/>,
    /// so <see cref="NbpOptions.Timeout"/> can shorten it but not extend it. <see langword="null"/> means no deadline.
    /// </summary>
    internal TimeSpan? SendTimeout
    {
        get
        {
            var deadline = Timeout;
            var clientTimeout = Finite(_httpClient.Timeout);
            if (deadline is null)
                return clientTimeout;
            if (clientTimeout is null)
                return deadline;
            return deadline < clientTimeout ? deadline : clientTimeout;
        }
    }

    private static TimeSpan? Finite(TimeSpan timeout)
        => timeout == System.Threading.Timeout.InfiniteTimeSpan ? null : timeout;

    /// <summary>
    /// Creates the connection for a client: base address from <paramref name="options"/>, then
    /// <see cref="HttpClient.BaseAddress"/>, then <see cref="NbpOptions.DefaultApiUrl"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The options or the <see cref="HttpClient.BaseAddress"/> are invalid.</exception>
    internal static NbpConnection Create(HttpClient httpClient, NbpOptions? options, NbpTelemetryProvider telemetryProvider, TimeProvider timeProvider)
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

        return new NbpConnection(httpClient, baseAddress, options?.Timeout, telemetryProvider, timeProvider);
    }

    /// <summary>
    /// Sends a GET request for <paramref name="relativePath"/>, resolved against the base address.
    /// A leading <c>/</c> is ignored, so the path never replaces the base address path (e.g. <c>/api/</c>).
    /// </summary>
    internal async Task<OpenUrzednikResult<TDto>> GetAsync<TDto>(string relativePath, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(typeInfo);

        var httpClient = _httpClient;
        var telemetryProvider = _telemetryProvider;
        var timeProvider = _timeProvider;
        var timeout = Timeout;
        var sendTimeout = SendTimeout;

        cancellationToken.ThrowIfCancellationRequested();

        using var traceSpan = telemetryProvider.Tracer.StartSpan("nbp.http.get");
        traceSpan.SetTag("http.path", relativePath);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_baseAddress, relativePath.TrimStart('/')));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

        // With ResponseHeadersRead, HttpClient.Timeout stops applying once the headers arrive,
        // so the deadline is applied to reading the body as well.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } deadline)
            timeoutSource.CancelAfter(deadline);
        var requestToken = timeoutSource.Token;

        var sendResult = await SendAsync(httpClient, request, relativePath, sendTimeout, telemetryProvider, traceSpan, requestToken, cancellationToken).ConfigureAwait(false);
        if (sendResult.IsFailure)
            return OpenUrzednikResult.Failure(sendResult.Errors);

        using var response = sendResult.Value;
        traceSpan.SetTag("http.status_code", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            return await MapErrorResponseAsync(response, relativePath, telemetryProvider, traceSpan, timeProvider, requestToken, cancellationToken).ConfigureAwait(false);

        try
        {
            var dto = await response.Content.ReadFromJsonAsync(typeInfo, requestToken).ConfigureAwait(false);
            if (dto is not null)
                return OpenUrzednikResult.Success(dto);

            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, null, "NBP API returned an empty response for {path}", "path", relativePath);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Empty response");
            return OpenUrzednikResult.Failure(new SerializationError($"NBP API returned an empty response for {relativePath}."));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return TimeoutFailure(timeout, relativePath, telemetryProvider, traceSpan, ex);
        }
        catch (JsonException ex)
        {
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to deserialize NBP response from {path}", "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure(new SerializationError($"Error when deserializing response from {relativePath}", ex));
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure(relativePath, telemetryProvider, traceSpan, ex);
        }
        catch (IOException ex)
        {
            return NetworkFailure(relativePath, telemetryProvider, traceSpan, ex);
        }
        catch (InvalidOperationException ex)
        {
            // ReadFromJsonAsync throws this when the response declares a charset it can't decode.
            telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to decode NBP response from {path}", "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure(new SerializationError($"Error when decoding response from {relativePath}", ex));
        }
    }

    private static async Task<OpenUrzednikResult<HttpResponseMessage>> SendAsync(HttpClient httpClient, HttpRequestMessage request, string relativePath, TimeSpan? timeout, NbpTelemetryProvider telemetryProvider, IOpenUrzednikSpan traceSpan, CancellationToken requestToken, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestToken)
                .ConfigureAwait(false);
            return OpenUrzednikResult.Success(response);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return TimeoutFailure(timeout, relativePath, telemetryProvider, traceSpan, ex);
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure(relativePath, telemetryProvider, traceSpan, ex);
        }
    }

    // The caller's token is not cancelled, so the cancellation came from HttpClient.Timeout or the request deadline.
    private static OpenUrzednikResult TimeoutFailure(TimeSpan? timeout, string relativePath, NbpTelemetryProvider telemetryProvider, IOpenUrzednikSpan traceSpan, OperationCanceledException exception)
    {
        telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, exception, "NBP request to {path} timed out after {timeout}", "path", relativePath, "timeout", timeout);
        var error = new RequestTimeoutError(
            timeout is null
                ? $"NBP request to {relativePath} timed out."
                : $"NBP API did not respond to {relativePath} within {timeout}.",
            timeout, exception);
        traceSpan.RecordException(exception);
        traceSpan.SetTag("error.code", error.Code);
        traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Timeout");
        return OpenUrzednikResult.Failure(error);
    }

    private static OpenUrzednikResult NetworkFailure(string relativePath, NbpTelemetryProvider telemetryProvider, IOpenUrzednikSpan traceSpan, Exception exception)
    {
        telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Warning, exception, "NBP request to {path} failed", "path", relativePath);
        var error = new ServiceUnavailableError(
            $"NBP API could not be reached for {relativePath}: {exception.Message}", exception: exception);
        traceSpan.RecordException(exception);
        traceSpan.SetTag("error.code", error.Code);
        traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        return OpenUrzednikResult.Failure(error);
    }

    private static async Task<OpenUrzednikResult> MapErrorResponseAsync(HttpResponseMessage response, string relativePath, NbpTelemetryProvider telemetryProvider, IOpenUrzednikSpan traceSpan, TimeProvider timeProvider, CancellationToken requestToken, CancellationToken cancellationToken)
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
                    var serverMessage = await ReadServerMessageAsync(response, telemetryProvider, requestToken, cancellationToken).ConfigureAwait(false);
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
    private static async Task<string?> ReadServerMessageAsync(HttpResponseMessage response, NbpTelemetryProvider telemetryProvider, CancellationToken requestToken, CancellationToken cancellationToken)
    {
        var buffer = new char[MaxServerMessageLength];
        var length = 0;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(requestToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            int read;
            while (length < buffer.Length &&
                   (read = await reader.ReadAsync(buffer.AsMemory(length), requestToken).ConfigureAwait(false)) > 0)
                length += read;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogUnreadableBody(telemetryProvider, ex);
            return null;
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
