using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

public partial class NbpExchangeRateTableClient
{
    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_latest");

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellLatestAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).Latest(), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_top_count");
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxDailyTablesTopCount).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellTopCountAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForTopCount(topCount), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_today");

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellTodayAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).Today(), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(date);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_date");
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(date), day, NbpCalendar.Today(_timeProvider)).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDate(day), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_range");
        traceSpan.SetTag("nbp.from", fromDay.ToIso8601String());
        traceSpan.SetTag("nbp.to", toDay.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(to), toDay, NbpCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator((fromDay, toDay), DateRangeValidator.MaxTablesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDateRange(fromDay, toDay), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }
}
