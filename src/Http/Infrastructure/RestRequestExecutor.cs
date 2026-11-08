using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Transport.Http;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Sends requests to a REST/JSON API and turns every response into an <see cref="OpenUrzednikResult{TValue}"/> (ADR-0002, ADR-0019).
/// For provider authors; applications use the provider clients.
/// </summary>
/// <remarks>
/// Requests use absolute URIs built from <see cref="BaseAddress"/>, so the <see cref="HttpClient"/> is never changed and can be shared.
/// Failures during normal execution are returned: non-success statuses, empty or malformed JSON, connection failures and timeouts.
/// Only caller cancellation throws <see cref="OperationCanceledException"/>.
/// </remarks>
internal sealed class RestRequestExecutor
{
    // The number of characters read from an error response body (ErrorResponseContext.ReadMessageAsync, 400 messages).
    internal const int MaxServerMessageLength = 500;

    // Not in netstandard2.0's HttpStatusCode and MediaTypeNames.
    private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
    private const string JsonMediaType = "application/json";

    // Timers can fire up to one system clock tick (about 15.6 ms on Windows) before their due time.
    internal static readonly TimeSpan TimerResolution = TimeSpan.FromMilliseconds(16);

    private readonly HttpClient _httpClient;
    private readonly TimeSpan? _timeout;
    private readonly RestProviderProfile _profile;
    private readonly OpenUrzednikTelemetry _telemetry;
    private readonly TimeProvider _timeProvider;
    private readonly string _spanName;
    // The span's server.address and server.port: request paths are relative, so the authority is the base address's.
    // Computed once (Uri.IdnHost allocates); the port is boxed once, because SetTag stores an object.
    private readonly string _serverAddress;
    private readonly object _serverPort;
    // The base address as text, for display paths only: its path (url.path) and the whole URI (status-error messages).
    // Built on first use, so a provider without display paths never pays for them.
    private string? _basePath;
    private string? _baseUri;

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
        // HasValue, not `timeout is { } value`, so that mutation testing checks the comparisons (ADR-0020): the pattern's `is not`
        // mutant leaves `value` unassigned where it's used (CS0165), so the mutant doesn't compile, and Stryker discards the other
        // mutants of that code with it.
        var value = timeout.GetValueOrDefault();
        if (timeout.HasValue && value != System.Threading.Timeout.InfiniteTimeSpan && (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(timeout), value, "The timeout must be positive and at most int.MaxValue ms, or Timeout.InfiniteTimeSpan.");

        _httpClient = httpClient;
        BaseAddress = baseAddress;
        _profile = profile;
        _timeout = timeout;
        _telemetry = telemetry ?? new OpenUrzednikTelemetry();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _spanName = profile.Name + ".http.get";
        _serverAddress = baseAddress.IdnHost;
        _serverPort = baseAddress.Port;
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
    public Task<OpenUrzednikResult<TDto>> GetAsync<TDto>(string relativePath, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken = default)
        => GetCoreAsync(relativePath, null, typeInfo, cancellationToken);

    /// <summary>
    /// Sends a GET request for <paramref name="relativePath"/>, like <see cref="GetAsync{TDto}(string, JsonTypeInfo{TDto}, CancellationToken)"/>,
    /// for a request whose path or query carries personal identifiers or secrets (ADR-0003, ADR-0019). <paramref name="displayPath"/> replaces
    /// the request URI everywhere the executor writes it: the <c>url.path</c> tag (the base address path plus the display path, without its query),
    /// logs, error messages and <see cref="ErrorResponseContext.RequestPath"/> (the base address plus the display path).
    /// The default mapping of a 400 leaves out the server's message, which may echo the identifier; a provider that needs it reads it in
    /// <see cref="RestProviderProfile.MapErrorAsync"/> through <see cref="ErrorResponseContext.ReadMessageAsync"/> and masks it.
    /// </summary>
    /// <typeparam name="TDto">The type of the deserialized body.</typeparam>
    /// <param name="relativePath">The path relative to <see cref="BaseAddress"/>, sent as given.</param>
    /// <param name="displayPath">
    /// The path and query written to telemetry and messages, in the same form as <paramref name="relativePath"/>: a route template
    /// (e.g. <c>search/nip/{nip}</c>, preferred for <c>url.path</c>) or the path with each identifier masked.
    /// </param>
    /// <param name="typeInfo">Source-generated metadata for <typeparamref name="TDto"/>.</param>
    /// <param name="cancellationToken">Cancels the request; cancellation throws <see cref="OperationCanceledException"/>.</param>
    /// <returns>The deserialized body, or the errors of a failed request.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="relativePath"/> or <paramref name="displayPath"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="relativePath"/>, <paramref name="displayPath"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public Task<OpenUrzednikResult<TDto>> GetAsync<TDto>(string relativePath, string displayPath, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayPath);
        return GetCoreAsync(relativePath, displayPath, typeInfo, cancellationToken);
    }

    // displayPath is null when the request URI carries nothing to hide: then nothing is allocated for display.
    private async Task<OpenUrzednikResult<TDto>> GetCoreAsync<TDto>(string relativePath, string? displayPath, JsonTypeInfo<TDto> typeInfo, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(typeInfo);

        cancellationToken.ThrowIfCancellationRequested();

        var requestUri = new Uri(BaseAddress, relativePath.TrimStart('/'));

        // OpenTelemetry HTTP client semantic conventions (ADR-0003): the address the client was given, before any DelegatingHandler
        // rewrites it. server.port is always set, as .NET 9+ does.
        using var traceSpan = _telemetry.TraceSource.StartSpan(_spanName);
        traceSpan.SetTag("http.request.method", "GET");
        traceSpan.SetTag("server.address", _serverAddress);
        traceSpan.SetTag("server.port", _serverPort);
        traceSpan.SetTag("url.path", displayPath is null ? requestUri.AbsolutePath : DisplayUrlPath(displayPath));

        // Expected failures are returned as errors; this only marks the span when something unexpected escapes,
        // without catching it (ADR-0002 forbids catch (Exception)).
        var completed = false;
        try
        {
            var result = await SendAndReadAsync(requestUri, displayPath ?? relativePath, displayPath, typeInfo, traceSpan, cancellationToken).ConfigureAwait(false);
            completed = true;
            return result;
        }
        finally
        {
            if (!completed && !cancellationToken.IsCancellationRequested)
                traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
        }
    }

    private async Task<OpenUrzednikResult<TDto>> SendAndReadAsync<TDto>(Uri requestUri, string messagePath, string? displayPath, JsonTypeInfo<TDto> typeInfo, IOpenUrzednikSpan traceSpan, CancellationToken cancellationToken)
    {
        var timeout = Timeout;

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));

        // The display URI replaces the request URI in status errors and in the DI package's HttpClient logging (ADR-0017).
        string? displayUri = null;
        if (displayPath is not null)
        {
            displayUri = DisplayUri(displayPath);
            DisplayUriRequestOption.Set(request, displayUri);
        }

        // With ResponseHeadersRead, HttpClient.Timeout stops applying once the headers arrive,
        // so the deadline is applied to reading the body as well.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // HasValue, not `timeout is { } deadline`, so that mutation testing checks CancelAfter: see the constructor (ADR-0020).
        if (timeout.HasValue)
            timeoutSource.CancelAfter(timeout.GetValueOrDefault());
        var requestToken = timeoutSource.Token;

        var sendResult = await SendAsync(request, messagePath, timeout, traceSpan, requestToken, cancellationToken).ConfigureAwait(false);
        if (!sendResult.TryGetValue(out var sentResponse))
            return OpenUrzednikResult.Failure<TDto>(sendResult.Errors);

        using var response = sentResponse;
        traceSpan.SetTag("http.response.status_code", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
            return await MapErrorResponseAsync(response, messagePath, displayUri, traceSpan, requestToken, cancellationToken).ConfigureAwait(false);

        var logger = _telemetry.Logger;
        try
        {
            // .NET Framework leaves Content null when there is no body; ReadJsonAsync handles that.
            var dto = await response.Content.ReadJsonAsync(typeInfo, requestToken).ConfigureAwait(false);
            if (dto is not null)
                return OpenUrzednikResult.Success(dto);

            logger.Log(OpenUrzednikLogLevel.Error, RestLogEvents.EmptyResponse, null, "{provider} returned an empty response for {path}", "provider", _profile.Name, "path", messagePath);
            traceSpan.SetTag("error.type", SerializationError.ErrorCode);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Empty response");
            return OpenUrzednikResult.Failure<TDto>(new SerializationError($"{_profile.DisplayName} returned an empty response for {messagePath}."));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return TimeoutFailure(timeout, messagePath, traceSpan, ex);
        }
        catch (JsonException ex)
        {
            logger.Log(OpenUrzednikLogLevel.Error, RestLogEvents.DeserializationFailed, ex, "Failed to deserialize {provider} response from {path}", "provider", _profile.Name, "path", messagePath);
            traceSpan.RecordException(ex);
            traceSpan.SetTag("error.type", SerializationError.ErrorCode);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure<TDto>(new SerializationError($"Error when deserializing response from {messagePath}", ex));
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure(messagePath, traceSpan, ex);
        }
        catch (IOException ex)
        {
            return NetworkFailure(messagePath, traceSpan, ex);
        }
        catch (InvalidOperationException ex)
        {
            // ReadFromJsonAsync throws this when the response declares a charset it can't decode.
            logger.Log(OpenUrzednikLogLevel.Error, RestLogEvents.DecodingFailed, ex, "Failed to decode {provider} response from {path}", "provider", _profile.Name, "path", messagePath);
            traceSpan.RecordException(ex);
            traceSpan.SetTag("error.type", SerializationError.ErrorCode);
            traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Deserialization failed");
            return OpenUrzednikResult.Failure<TDto>(new SerializationError($"Error when decoding response from {messagePath}", ex));
        }
    }

    private async Task<OpenUrzednikResult<HttpResponseMessage>> SendAsync(HttpRequestMessage request, string messagePath, TimeSpan? deadline, IOpenUrzednikSpan traceSpan, CancellationToken requestToken, CancellationToken cancellationToken)
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
            return TimeoutFailure(ElapsedLimit(deadline, requestToken, started), messagePath, traceSpan, ex);
        }
        catch (HttpRequestException ex)
        {
            return NetworkFailure(messagePath, traceSpan, ex);
        }
    }

    // Names only a limit known to have elapsed. The request deadline cancels requestToken. HttpClient.Timeout counts when at least
    // that long went by: it runs on the real clock, as Stopwatch does, and .NET Framework gives it no inner TimeoutException.
    // Anything shorter, e.g. a resilience handler's timeout or a connect timeout, is a limit we don't know.
    private TimeSpan? ElapsedLimit(TimeSpan? deadline, CancellationToken requestToken, long started)
    {
        if (requestToken.IsCancellationRequested)
            return deadline;

        // HasValue, not `is { } clientTimeout`, so that mutation testing checks which limit is named: see the constructor (ADR-0020).
        var clientTimeout = Finite(_httpClient.Timeout);
        return clientTimeout.HasValue && LimitElapsed(Stopwatch.GetElapsedTime(started), clientTimeout.GetValueOrDefault()) ? clientTimeout : null;
    }

    // Whether a limit counts as elapsed: its timer may fire up to TimerResolution early, so an elapsed time that close counts too.
    // A function of its own so the boundary is tested exactly, without a real clock.
    internal static bool LimitElapsed(TimeSpan elapsed, TimeSpan limit) => elapsed + TimerResolution >= limit;

    // The caller's token is not cancelled, so the cancellation came from the request deadline, HttpClient.Timeout
    // or a handler's own timeout; timeout is null when the limit isn't known.
    private OpenUrzednikResult TimeoutFailure(TimeSpan? timeout, string messagePath, IOpenUrzednikSpan traceSpan, OperationCanceledException exception)
    {
        if (timeout is null)
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestTimedOut, exception, "{provider} request to {path} timed out",
                "provider", _profile.Name, "path", messagePath);
        else
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestTimedOut, exception, "{provider} request to {path} timed out after {timeout}",
                "provider", _profile.Name, "path", messagePath, "timeout", timeout);
        var error = new RequestTimeoutError(
            timeout is null
                ? $"{_profile.DisplayName} request to {messagePath} timed out."
                : $"{_profile.DisplayName} did not respond to {messagePath} within {timeout}.",
            timeout, exception);
        traceSpan.RecordException(exception);
        traceSpan.SetTag("error.type", error.Code);
        traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Timeout");
        return error;
    }

    // A DNS, TLS or connection failure, or a broken body read, is a ConnectionFailedError. A resilience handler's rejection (open circuit
    // breaker, rate limiter) also arrives as an HttpRequestException, marked by the DI package, and stays a ServiceUnavailableError.
    private OpenUrzednikResult NetworkFailure(string messagePath, IOpenUrzednikSpan traceSpan, Exception exception)
    {
        var rejected = ResilienceRejection.IsRejection(exception);
        if (rejected)
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestRejected, exception, "{provider} request to {path} was rejected", "provider", _profile.Name, "path", messagePath);
        else
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ConnectionFailed, exception, "{provider} could not be reached for {path}", "provider", _profile.Name, "path", messagePath);
        OpenUrzednikError error = rejected
            ? new ServiceUnavailableError($"{_profile.DisplayName} request to {messagePath} was rejected: {exception.Message}", exception: exception)
            : new ConnectionFailedError($"{_profile.DisplayName} could not be reached for {messagePath}: {exception.Message}", exception);
        traceSpan.RecordException(exception);
        traceSpan.SetTag("error.type", error.Code);
        traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, rejected ? "Rejected" : "Network failure");
        return error;
    }

    private async Task<OpenUrzednikResult> MapErrorResponseAsync(HttpResponseMessage response, string messagePath, string? displayUri, IOpenUrzednikSpan traceSpan, CancellationToken requestToken, CancellationToken cancellationToken)
    {
        var path = displayUri ?? response.RequestMessage?.RequestUri?.ToString() ?? messagePath;
        var delay = response.StatusCode == TooManyRequests ? GetDelay(response, _timeProvider) : null;

        // The body can be read only once, so the override and the default mapping share one read.
        Task<string?>? messageTask = null;
        Task<string?> ReadMessageOnceAsync() => messageTask ??= ReadServerMessageAsync(response, requestToken, cancellationToken);

        // Locals and `is not null`, not pattern variables, so that mutation testing checks this code: see the constructor (ADR-0020).
        var mapErrorAsync = _profile.MapErrorAsync;
        if (mapErrorAsync is not null)
        {
            var context = new ErrorResponseContext(response, path, delay, ReadMessageOnceAsync, requestToken);
            var customError = await mapErrorAsync(context).ConfigureAwait(false);
            if (customError is not null)
            {
                _telemetry.Logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ErrorResponse, null, "{provider} returned status {status} for {path}",
                    "provider", _profile.Name, "status", (int)response.StatusCode, "path", path);
                traceSpan.RecordError(customError);
                return customError;
            }
        }

        // A display path means the request carries identifiers, which a 400's server message may echo (ADR-0019).
        var result = await MapDefaultErrorAsync(response, path, delay, traceSpan, displayUri is null ? ReadMessageOnceAsync : null).ConfigureAwait(false);
        traceSpan.SetTag("error.type", result.Errors[0].Code);
        return result;
    }

    // readMessageAsync is null when a 400's server message must be left out.
    private async Task<OpenUrzednikResult> MapDefaultErrorAsync(HttpResponseMessage response, string path, TimeSpan? delay, IOpenUrzednikSpan traceSpan, Func<Task<string?>>? readMessageAsync)
    {
        var statusCode = (int)response.StatusCode;
        var logger = _telemetry.Logger;
        var displayName = _profile.DisplayName;
        var name = _profile.Name;

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                {
                    if (logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                        logger.Log(OpenUrzednikLogLevel.Debug, RestLogEvents.NotFound, null, "{provider} resource not found: {path}.", "provider", name, "path", path);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Not found");
                    return new HttpNotFoundError($"Resource at {path} was not found.", response.StatusCode);
                }
            case TooManyRequests:
                {
                    if (logger.IsEnabled(OpenUrzednikLogLevel.Warning))
                    {
                        if (delay.HasValue)
                            logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RateLimited, null, "{provider} rate limit hit, retry after {delay}.", "provider", name, "delay", delay);
                        else
                            logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RateLimited, null, "{provider} rate limit hit.", "provider", name);
                    }
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Rate limited");
                    return new HttpRateLimitExceededError("Too many requests.", delay, response.StatusCode);
                }
            case HttpStatusCode.BadRequest:
                {
                    var serverMessage = readMessageAsync is null ? null : await readMessageAsync().ConfigureAwait(false);
                    logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.BadRequest, null, "{provider} rejected the request to {path}", "provider", name, "path", path);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Bad request");
                    return new HttpBadRequestError(
                        serverMessage is null
                            ? $"{displayName} rejected the request to {path}."
                            : $"{displayName} rejected the request to {path}: {serverMessage}",
                        response.StatusCode);
                }
            case HttpStatusCode.RequestTimeout:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.RequestTimeoutStatus, null, "{provider} timed out on {path} with status {status}", "provider", name, "path", path, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Timeout");
                    return new HttpRequestTimeoutError($"{displayName} timed out on {path} with status {statusCode}.", response.StatusCode);
                }
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.Unauthorized, null, "{provider} denied access to {path} with status {status}", "provider", name, "path", path, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unauthorized");
                    return new HttpUnauthorizedError($"{displayName} denied access to {path} with status {statusCode}.", response.StatusCode);
                }
            case >= HttpStatusCode.InternalServerError:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.ServiceUnavailable, null, "{provider} is unavailable, status {status}", "provider", name, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Service unavailable");
                    return new HttpServiceUnavailableError($"{displayName} is unavailable, status {statusCode}.", response.StatusCode);
                }
            default:
                {
                    logger.Log(OpenUrzednikLogLevel.Warning, RestLogEvents.UnexpectedStatus, null, "{provider} returned unexpected status {status}", "provider", name, "status", statusCode);
                    traceSpan.SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected status");
                    return new HttpUnknownError($"{displayName} returned unexpected status {statusCode}.", response.StatusCode);
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
            // The same loop as `while (length < buffer.Length && (read = await …) > 0)`, written so that mutation testing can check it
            // (ADR-0020): with the assignment inside the condition, one mutant leaves `read` unassigned and Stryker skips the whole
            // method, and `<` would leave an equivalent `<=` mutant. A read never fills past the buffer, so `!=` stops at the same point.
            while (length != buffer.Length)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(length), requestToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                length += read;
            }
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

    // The url.path of a display path: the base address path, then the display path without leading '/' and without its query.
    private string DisplayUrlPath(string displayPath)
    {
        var start = SkipLeadingSlashes(displayPath);
        var query = displayPath.IndexOf('?', start);
        var end = query < 0 ? displayPath.Length : query;
        // Stryker disable once Assignment : equivalent, ??= only caches the base path; assigning it on every call gives the same text
        return string.Concat((_basePath ??= BaseAddress.AbsolutePath).AsSpan(), displayPath.AsSpan(start, end - start));
    }

    // The status-error path of a display path: the base address, then the display path without leading '/', as GetAsync resolves it.
    // Stryker disable once Assignment : equivalent, ??= only caches the base address text; assigning it on every call gives the same text
    private string DisplayUri(string displayPath)
        => string.Concat((_baseUri ??= BaseAddress.ToString()).AsSpan(), displayPath.AsSpan(SkipLeadingSlashes(displayPath)));

    private static int SkipLeadingSlashes(string path)
    {
        var start = 0;
        while (start < path.Length && path[start] == '/')
            start++;
        return start;
    }

    private void LogUnreadableBody(Exception exception)
    {
        if (_telemetry.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
            _telemetry.Logger.Log(OpenUrzednikLogLevel.Debug, RestLogEvents.ErrorBodyUnreadable, exception, "Could not read {provider} error response body.", "provider", _profile.Name);
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
