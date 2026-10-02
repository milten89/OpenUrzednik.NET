using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

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

        var validation = new TopCountValidator(nameof(topCount), topCount).Validate();

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
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_date");
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(date), date, NbpCalendar.Today(_timeProvider)).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDate(date), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.buy_sell_range");
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(to), to, NbpCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator((from, to), DateRangeValidator.MaxTablesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDateRange(from, to), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }
}
