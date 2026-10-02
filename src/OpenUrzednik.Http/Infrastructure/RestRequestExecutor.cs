using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Sends requests to a REST/JSON API and turns every response into an <see cref="OpenUrzednikResult{TValue}"/> (ADR-0002, ADR-0006).
/// For provider authors; applications use the provider clients.
/// </summary>
/// <remarks>
/// Requests use absolute URIs built from <see cref="BaseAddress"/>, so the <see cref="HttpClient"/> is never changed and can be shared.
/// Failures during normal execution are returned: non-success statuses, empty or malformed JSON, network errors and timeouts.
/// Only caller cancellation throws <see cref="OperationCanceledException"/>.
/// </remarks>
public sealed class RestRequestExecutor
{
    // The number of characters read from an error response body (ErrorResponseContext.ReadMessageAsync, 400 messages).
    internal const int MaxServerMessageLength = 500;

    // Not in netstandard2.0's HttpStatusCode and MediaTypeNames.
    private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
    private const string JsonMediaType = "application/json";

    // Timers can fire up to one system clock tick (about 15.6 ms on Windows) before their due time.
    private static readonly TimeSpan TimerResolution = TimeSpan.FromMilliseconds(16);

    private readonly HttpClient _httpClient;
    private readonly TimeSpan? _timeout;
    private readonly RestProviderProfile _profile;
    private readonly OpenUrzednikTelemetry _telemetry;
    private readonly TimeProvider _timeProvider;
    private readonly string _spanName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestRequestExecutor"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client. It isn't changed.</param>
    /// <param name="baseAddress">Absolute base address of the API, ending with <c>/</c>. Request paths are resolved against it.</param>
    /// <param name="profile">The provider's names and error mapping override.</param>
    /// <param name="timeout">
    /// Deadline for one request, including reading the body. <see langword="null"/> uses <see cref="HttpClient.Timeout"/>;
    /// <see cref="Timeout.InfiniteTimeSpan"/> removes this deadline. <see cref="HttpClient.Timeout"/> still limits the wait for the headers.
    /// </param>
    /// <param name="telemetry">Logger and trace source; <see langword="null"/> for no-op telemetry.</param>
    /// <param name="timeProvider">Clock for <c>Retry-After</c> dates; <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/>, <paramref name="baseAddress"/> or <paramref name="profile"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> isn't absolute.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> isn't positive, isn't at most <see cref="int.MaxValue"/> milliseconds, and isn't <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
    public RestRequestExecutor(HttpClient httpClient, Uri baseAddress, RestProviderProfile profile, TimeSpan? timeout = null,
        OpenUrzednikTelemetry? telemetry = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(profile);
        if (!baseAddress.IsAbsoluteUri)
            throw new ArgumentException($"The base address must be absolute: {baseAddress}", nameof(baseAddress));
        if (timeout is { } value && value != System.Threading.Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(timeout), value, "The timeout must be positive and at most int.MaxValue ms, or Timeout.InfiniteTimeSpan.");

        _httpClient = httpClient;
        BaseAddress = baseAddress;
        _profile = profile;
        _timeout = timeout;
        _telemetry = telemetry ?? new OpenUrzednikTelemetry();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _spanName = profile.Name + ".http.get";
    }

    /// <summary>
    /// Gets the base address request paths are resolved against.
    /// </summary>
    public Uri BaseAddress { get; }

    /// <summary>
    /// Gets the request deadline: the timeout passed to the constructor when set, otherwise <see cref="HttpClient.Timeout"/>.
    /// It also covers reading the body, which <see cref="HttpClient.Timeout"/> doesn't with <see cref="HttpCompletionOption.ResponseHeadersRead"/>.
    /// <see langword="null"/> means no deadline.
    /// </summary>
    public TimeSpan? Timeout => Finite(_timeout ?? _httpClient.Timeout);

    private static TimeSpan? Finite(TimeSpan timeout)
        => timeout == System.Threading.Timeout.InfiniteTimeSpan ? null : timeout;

    /// <summary>
    /// Sends a GET request for <paramref name="relativePath"/>, resolved against <see cref="BaseAddress"/>, and deserializes the JSON body.
    /// A leading <c>/</c> is ignored, so the path never replaces the base address path (e.g. <c>/api/</c>).
    /// </summary>
    /// <typeparam name="TDto">The type of the deserialized body.</typeparam>
    /// <param name="relativePath">The path relative to <see cref="BaseAddress"/>.</param>
    /// <param name="typeInfo">Source-generated metadata for <typeparamref name="TDto"/>.</param>
    /// <param name="cancellationToken">Cancels the request; cancellation throws <see cref="OperationCanceledException"/>.</param>
    /// <returns>The deserialized body, or the errors of a failed request.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="relativePath"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="relativePath"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task<OpenUrzednikResult<TDto>> GetAsync<TDto>(string relativePath, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(typeInfo);

        cancellationToken.ThrowIfCancellationRequested();

        var requestUri = new Uri(BaseAddress, relativePath.TrimStart('/'));

        // OpenTelemetry HTTP semantic conventions (ADR-0003).
        using var traceSpan = _telemetry.TraceSource.StartSpan(_spanName);
        traceSpan.SetTag("http.request.method", "GET");
        traceSpan.SetTag("url.path", requestUri.AbsolutePath);

        // Expected failures are returned as errors; this only marks the span when something unexpected escapes,
        // without catching it (ADR-0002 forbids catch (Exception)).
        var completed = false;
        try
        {
            var result = await SendAndReadAsync(requestUri, relativePath, typeInfo, traceSpan, cancellationToken).ConfigureAwait(false);
            completed = true;
            return result;
        }
        finally
        {
            if (!completed && !cancellationToken.IsCancellationRequested)
                traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
        }
    }

    private async Task<OpenUrzednikResult<TDto>> SendAndReadAsync<TDto>(Uri requestUri, string relativePath, JsonTypeInfo<TDto> typeInfo, IOpenUrzednikSpan traceSpan, CancellationToken cancellationToken)
    {
        var timeout = Timeout;

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));

        // With ResponseHeadersRead, HttpClient.Timeout stops applying once the headers arrive,
        // so the deadline is applied to reading the body as well.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } deadline)
            timeoutSource.CancelAfter(deadline);
        var requestToken = timeoutSource.Token;

        var sendResult = await SendAsync(request, relativePath, timeout, traceSpan, requestToken, cancellationToken).ConfigureAwait(false);
        if (!sendResult.TryGetValue(out var sentResponse))
            return OpenUrzednikResult.Failure<TDto>(sendResult.Errors);

        using var response = sentResponse;
        traceSpan.SetTag("http.response.status_code", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            return await MapErrorResponseAsync(response, relativePath, traceSpan, requestToken, cancellationToken).ConfigureAwait(false);

        var logger = _telemetry.Logger;
        try
        {
            // .NET Framework leaves Content null when there is no body; .NET always sets one.
            var dto = response.Content is null ? default : await response.Content.ReadFromJsonAsync(typeInfo, requestToken).ConfigureAwait(false);
            if (dto is not null)
                return OpenUrzednikResult.Success(dto);

            logger.Log(OpenUrzednikLogLevel.Error, null, "{provider} returned an empty response for {path}", "provider", _profile.DisplayName, "path", relativePath);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Empty response");
            return OpenUrzednikResult.Failure<TDto>(new SerializationError($"{_profile.DisplayName} returned an empty response for {relativePath}."));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return TimeoutFailure(timeout, relativePath, traceSpan, ex);
        }
        catch (JsonException ex)
        {
            logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to deserialize {provider} response from {path}", "provider", _profile.DisplayName, "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure<TDto>(new SerializationError($"Error when deserializing response from {relativePath}", ex));
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure(relativePath, traceSpan, ex);
        }
        catch (IOException ex)
        {
            return NetworkFailure(relativePath, traceSpan, ex);
        }
        catch (InvalidOperationException ex)
        {
            // ReadFromJsonAsync throws this when the response declares a charset it can't decode.
            logger.Log(OpenUrzednikLogLevel.Error, ex, "Failed to decode {provider} response from {path}", "provider", _profile.DisplayName, "path", relativePath);
            traceSpan.RecordException(ex);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure<TDto>(new SerializationError($"Error when decoding response from {relativePath}", ex));
        }
    }

    private async Task<OpenUrzednikResult<HttpResponseMessage>> SendAsync(HttpRequestMessage request, string relativePath, TimeSpan? deadline, IOpenUrzednikSpan traceSpan, CancellationToken requestToken, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestToken)
                .ConfigureAwait(false);
            return OpenUrzednikResult.Success(response);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return TimeoutFailure(ElapsedLimit(deadline, requestToken, started), relativePath, traceSpan, ex);
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure(relativePath, traceSpan, ex);
        }
    }

    // Names only a limit known to have elapsed. The request deadline cancels requestToken. HttpClient.Timeout counts when at least
    // that long went by: it runs on the real clock, as Stopwatch does, and .NET Framework gives it no inner TimeoutException.
    // Anything shorter, e.g. a resilience handler's timeout or a connect timeout, is a limit we don't know.
    private TimeSpan? ElapsedLimit(TimeSpan? deadline, CancellationToken requestToken, long started)
    {
        if (requestToken.IsCancellationRequested)
            return deadline;

        var elapsed = TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - started) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
        return Finite(_httpClient.Timeout) is { } clientTimeout && elapsed + TimerResolution >= clientTimeout ? clientTimeout : null;
    }

    // The caller's token is not cancelled, so the cancellation came from the request deadline, HttpClient.Timeout
    // or a handler's own timeout; timeout is null when the limit isn't known.
    private OpenUrzednikResult TimeoutFailure(TimeSpan? timeout, string relativePath, IOpenUrzednikSpan traceSpan, OperationCanceledException exception)
    {
        if (timeout is null)
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, exception, "{provider} request to {path} timed out",
                "provider", _profile.DisplayName, "path", relativePath);
        else
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, exception, "{provider} request to {path} timed out after {timeout}",
                "provider", _profile.DisplayName, "path", relativePath, "timeout", timeout);
        var error = new RequestTimeoutError(
            timeout is null
                ? $"{_profile.DisplayName} request to {relativePath} timed out."
                : $"{_profile.DisplayName} did not respond to {relativePath} within {timeout}.",
            timeout, exception);
        traceSpan.RecordException(exception);
        traceSpan.SetTag("error.code", error.Code);
        traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Timeout");
        return error;
    }

    private OpenUrzednikResult NetworkFailure(string relativePath, IOpenUrzednikSpan traceSpan, Exception exception)
    {
        _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, exception, "{provider} request to {path} failed", "provider", _profile.DisplayName, "path", relativePath);
        var error = new ServiceUnavailableError(
            $"{_profile.DisplayName} could not be reached for {relativePath}: {exception.Message}", exception: exception);
        traceSpan.RecordException(exception);
        traceSpan.SetTag("error.code", error.Code);
        traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Network failure");
        return error;
    }

    private async Task<OpenUrzednikResult> MapErrorResponseAsync(HttpResponseMessage response, string relativePath, IOpenUrzednikSpan traceSpan, CancellationToken requestToken, CancellationToken cancellationToken)
    {
        var path = response.RequestMessage?.RequestUri?.ToString() ?? relativePath;
        var delay = response.StatusCode == TooManyRequests ? GetDelay(response, _timeProvider) : null;

        // The body can be read only once, so the override and the default mapping share one read.
        Task<string?>? messageTask = null;
        Task<string?> ReadMessageOnceAsync() => messageTask ??= ReadServerMessageAsync(response, requestToken, cancellationToken);

        if (_profile.MapErrorAsync is { } mapErrorAsync)
        {
            var context = new ErrorResponseContext(response, path, delay, ReadMessageOnceAsync, requestToken);
            if (await mapErrorAsync(context).ConfigureAwait(false) is { } customError)
            {
                _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} returned status {status} for {path}",
                    "provider", _profile.DisplayName, "status", (int)response.StatusCode, "path", path);
                traceSpan.RecordError(customError);
                return customError;
            }
        }

        return await MapDefaultErrorAsync(response, path, delay, traceSpan, ReadMessageOnceAsync).ConfigureAwait(false);
    }

    private async Task<OpenUrzednikResult> MapDefaultErrorAsync(HttpResponseMessage response, string path, TimeSpan? delay, IOpenUrzednikSpan traceSpan, Func<Task<string?>> readMessageAsync)
    {
        var statusCode = (int)response.StatusCode;
        var logger = _telemetry.Logger;
        var displayName = _profile.DisplayName;

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                {
                    if (logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                        logger.Log(OpenUrzednikLogLevel.Debug, null, "{provider} resource not found: {path}.", "provider", displayName, "path", path);
                    return new NotFoundError($"Resource at {path} was not found.", statusCode);
                }
            case TooManyRequests:
                {
                    if (logger.IsEnabled(OpenUrzednikLogLevel.Warning))
                    {
                        if (delay.HasValue)
                            logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} rate limit hit, retry after {delay}.", "provider", displayName, "delay", delay);
                        else
                            logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} rate limit hit.", "provider", displayName);
                    }
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Rate limited");
                    return new RateLimitExceededError("Too many requests.", delay, statusCode);
                }
            case HttpStatusCode.BadRequest:
                {
                    var serverMessage = await readMessageAsync().ConfigureAwait(false);
                    logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} rejected the request to {path}", "provider", displayName, "path", path);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Bad request");
                    return new BadRequestError(
                        serverMessage is null
                            ? $"{displayName} rejected the request to {path}."
                            : $"{displayName} rejected the request to {path}: {serverMessage}",
                        statusCode);
                }
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} denied access to {path} with status {status}", "provider", displayName, "path", path, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unauthorized");
                    return new UnauthorizedError($"{displayName} denied access to {path} with status {statusCode}.", statusCode);
                }
            case >= HttpStatusCode.InternalServerError:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} is unavailable, status {status}", "provider", displayName, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Service unavailable");
                    return new ServiceUnavailableError($"{displayName} is unavailable, status {statusCode}.", statusCode);
                }
            default:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, null, "{provider} returned unexpected status {status}", "provider", displayName, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected status");
                    return new UnknownError($"{displayName} returned unexpected status {statusCode}.", statusCode);
                }
        }
    }

    /// <summary>
    /// Reads the plain-text message an API returns with an error response (e.g. NBP's <c>400 BadRequest - Błędny zakres dat</c>).
    /// At most <see cref="MaxServerMessageLength"/> characters are read, so a large body (e.g. an HTML page from a proxy) is not buffered.
    /// Returns <see langword="null"/> when there is no body or it can't be read, because the message is only extra context.
    /// </summary>
    private async Task<string?> ReadServerMessageAsync(HttpResponseMessage response, CancellationToken requestToken, CancellationToken cancellationToken)
    {
        // .NET Framework leaves Content null when there is no body.
        if (response.Content is null)
            return null;

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
            LogUnreadableBody(ex);
            return null;
        }
        catch (HttpRequestException ex)
        {
            LogUnreadableBody(ex);
            return null;
        }
        catch (IOException ex)
        {
            LogUnreadableBody(ex);
            return null;
        }

        var message = new string(buffer, 0, length).Trim();
        return message.Length == 0 ? null : message;
    }

    private void LogUnreadableBody(Exception exception)
    {
        if (_telemetry.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Debug, exception, "Could not read {provider} error response body.", "provider", _profile.DisplayName);
    }

    private static TimeSpan? GetDelay(HttpResponseMessage response, TimeProvider timeProvider)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
            return null;

        if (retryAfter.Delta.HasValue)
            return retryAfter.Delta.Value;

        if (retryAfter.Date.HasValue)
        {
            var responseDate = response.Headers.Date ?? timeProvider.GetUtcNow();
            return retryAfter.Date.Value - responseDate;
        }

        return null;
    }
}
