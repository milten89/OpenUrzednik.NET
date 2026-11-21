using OpenUrzednik.Core;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.MF.WhiteList.Validation;

namespace OpenUrzednik.MF.WhiteList;

/// <inheritdoc cref="IWhiteListClient"/>
public sealed partial class WhiteListClient : IWhiteListClient
{
    private readonly WhiteListRequestPipeline _pipeline;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListClient"/> class with the default settings: the production host
    /// <see cref="WhiteListOptions.DefaultApiUrl"/> (or <see cref="HttpClient.BaseAddress"/>, when set) and the <see cref="HttpClient"/>'s
    /// own timeout.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The <see cref="HttpClient.BaseAddress"/> is invalid (see <see cref="WhiteListOptions.ApiUrl"/>).</exception>
    public WhiteListClient(HttpClient httpClient)
        : this(httpClient, options: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    /// <param name="options">Host root and timeout; <see langword="null"/> uses the defaults (see <see cref="WhiteListOptions"/>).</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date) in the date checks, the <c>RetryAfter</c> estimate of a 429 and
    /// the duration of untraced operations in the metrics; <see langword="null"/> uses <see cref="TimeProvider.System"/>.</param>
    /// <param name="telemetry">Logger, trace source and metrics; <see langword="null"/>, or a property left <see langword="null"/>, uses the
    /// default: no logging, and the <see cref="WhiteListTelemetry.SourceName"/> <c>ActivitySource</c> and <c>Meter</c> on .NET 8 and later
    /// (none on .NET Framework, netstandard2.0) (ADR-0025).</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute
    /// <c>https</c> URL, a path ending with <c>api/</c> or <c>api2/</c>, or a non-positive timeout; see <see cref="WhiteListOptions"/>).</exception>
    public WhiteListClient(HttpClient httpClient, WhiteListOptions? options = null, TimeProvider? timeProvider = null, OpenUrzednikTelemetry? telemetry = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        var clientTelemetry = new ClientTelemetry(telemetry?.Logger, telemetry?.TraceSource, WhiteListTelemetry.SourceName, telemetry?.Metrics);
        _pipeline = new WhiteListRequestPipeline(WhiteListConnection.CreateSearch(httpClient, options, clientTelemetry, _timeProvider), clientTelemetry);
    }

    // The steps the three batch searches share (ADR-0023): one operation, the count and the date as tags (never the identifiers), the
    // identifiers and the date validated together, the normalized identifiers joined with commas into the path.
    private async Task<OpenUrzednikResult<BatchSearchResult>> SearchBatchCoreAsync(string spanName, string methodName, WhiteListBatchValidator validator,
        DateTime date, string path, string displayPath, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation(spanName);
        var traceSpan = operation.Span;
        var count = validator.Value.Count;
        traceSpan.SetTag("mf_white_list.count", count);
        var dayText = day.ToIso8601String();
        traceSpan.SetTag("mf_white_list.date", dayText);

        var validation = validator.Validate()
            .And(new WhiteListDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate());

        var result = await _pipeline.GetAsync(traceSpan, methodName, validation,
            () => $"{path}{validator.Joined()}?date={dayText}", displayPath,
            WhiteListJsonContext.Default.EntryListResponseDto, dto => Mapper.MapToBatchSearchResult(dto, count), cancellationToken).ConfigureAwait(false);

        // While the operation's span is still current (ADR-0015).
        if (result.IsSuccess)
            _pipeline.LogFailedEntries(methodName, result.Value.Entries);

        return operation.Complete(result);
    }
}
