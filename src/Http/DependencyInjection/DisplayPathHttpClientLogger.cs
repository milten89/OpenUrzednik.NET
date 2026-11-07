using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

using OpenUrzednik.Http.Infrastructure;

namespace OpenUrzednik.Http.DependencyInjection;

/// <summary>
/// Writes the request start, end and failure entries of <c>IHttpClientFactory</c>'s default loggers (.NET 10: the same categories,
/// event ids, names, levels and messages), with the display URI the executor attached instead of the request URI (ADR-0017).
/// A request without one is logged as .NET 9 and later redact it: the path is kept, a query becomes <c>?*</c>.
/// </summary>
internal sealed class DisplayPathHttpClientLogger : IHttpClientLogger
{
    // The templates, ids and names of Microsoft.Extensions.Http's LogHelper (dotnet/runtime, release/10.0). LogRequestStart checks
    // IsEnabled itself, before building the URI, so its delegates skip the second check.
    // Stryker disable once Boolean : performance only, a second IsEnabled check changes no entry
    private static readonly LogDefineOptions SkipEnabledCheck = new() { SkipEnabledCheck = true };
    // The names and templates are false survivors in mutation testing: a static initializer runs once per test host, so Stryker
    // activates its mutants only after a host restart (order-dependent). Replaced by hand, each one fails the tests.
    // Stryker disable once String : static initializer, see above
    private static readonly Action<ILogger, HttpMethod, string?, Exception?> PipelineStart = LoggerMessage.Define<HttpMethod, string?>(
        LogLevel.Information, new EventId(100, "RequestPipelineStart"), "Start processing HTTP request {HttpMethod} {Uri}", SkipEnabledCheck);
    // Stryker disable once String : static initializer, see above
    private static readonly Action<ILogger, double, int, Exception?> PipelineEnd = LoggerMessage.Define<double, int>(
        LogLevel.Information, new EventId(101, "RequestPipelineEnd"), "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}");
    // Stryker disable once String : static initializer, see above
    private static readonly Action<ILogger, double, Exception?> PipelineFailed = LoggerMessage.Define<double>(
        LogLevel.Information, new EventId(104, "RequestPipelineFailed"), "HTTP request failed after {ElapsedMilliseconds}ms");
    // Stryker disable once String : static initializer, see above
    private static readonly Action<ILogger, HttpMethod, string?, Exception?> ClientStart = LoggerMessage.Define<HttpMethod, string?>(
        LogLevel.Information, new EventId(100, "RequestStart"), "Sending HTTP request {HttpMethod} {Uri}", SkipEnabledCheck);
    // Stryker disable once String : static initializer, see above
    private static readonly Action<ILogger, double, int, Exception?> ClientEnd = LoggerMessage.Define<double, int>(
        LogLevel.Information, new EventId(101, "RequestEnd"), "Received HTTP response headers after {ElapsedMilliseconds}ms - {StatusCode}");
    // Stryker disable once String : static initializer, see above
    private static readonly Action<ILogger, double, Exception?> ClientFailed = LoggerMessage.Define<double>(
        LogLevel.Information, new EventId(104, "RequestFailed"), "HTTP request failed after {ElapsedMilliseconds}ms");

    private readonly ILogger _logger;
    private readonly bool _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayPathHttpClientLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger of the <c>LogicalHandler</c> or <c>ClientHandler</c> category.</param>
    /// <param name="pipeline"><see langword="true"/> for the entries around the whole handler pipeline (<c>LogicalHandler</c>).</param>
    internal DisplayPathHttpClientLogger(ILogger logger, bool pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;
    }

    /// <inheritdoc />
    public object? LogRequestStart(HttpRequestMessage request)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return null;

        var uri = DisplayUriRequestOption.TryGet(request, out var displayUri) ? RedactQuery(displayUri) : RedactUri(request.RequestUri);
        (_pipeline ? PipelineStart : ClientStart)(_logger, request.Method, uri, null);
        return null;
    }

    /// <inheritdoc />
    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
        => (_pipeline ? PipelineEnd : ClientEnd)(_logger, elapsed.TotalMilliseconds, (int)response.StatusCode, null);

    /// <inheritdoc />
    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        // .NET 10 logs only an HttpRequestException; a timeout or cancellation isn't logged here (the executor logs a timeout).
        if (exception is HttpRequestException)
            (_pipeline ? PipelineFailed : ClientFailed)(_logger, elapsed.TotalMilliseconds, exception);
    }

    /// <summary>
    /// Replaces a non-empty query of a display URI with <c>*</c>, as .NET 9 and later do for request URIs: a display path may keep
    /// a value in its query that isn't for logs.
    /// </summary>
    internal static string RedactQuery(string displayUri)
    {
        var query = displayUri.IndexOf('?');
        return query < 0 || query == displayUri.Length - 1 ? displayUri : displayUri.Remove(query + 1) + "*";
    }

    /// <summary>
    /// Redacts a URI as .NET 9 and later do in these entries, also when an app turns their redaction off: <c>scheme://host[:port]path</c>, a query becomes <c>?*</c>,
    /// user info and the fragment are dropped, and a relative URI becomes <c>*</c>.
    /// </summary>
    internal static string? RedactUri(Uri? uri)
    {
        if (uri is null)
            return null;
        if (!uri.IsAbsoluteUri)
            return "*";

        var authority = uri.IsDefaultPort ? uri.Host : string.Concat(uri.Host, ":", uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var path = uri.AbsolutePath;
        return uri.Query.Length > 1
            ? string.Concat(uri.Scheme, "://", authority, path, "?*")
            : string.Concat(uri.Scheme, "://", authority, path);
    }
}
