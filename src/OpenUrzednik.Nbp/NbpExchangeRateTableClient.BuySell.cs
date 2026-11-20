using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

public sealed partial class NbpExchangeRateTableClient
{
    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default)
    {
        using var operation = _pipeline.StartOperation("nbp.table.buy_sell_latest");
        var traceSpan = operation.Span;

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellLatestAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).Latest(), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        using var operation = _pipeline.StartOperation("nbp.table.buy_sell_top_count");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxDailyTablesTopCount).Validate();

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(GetBuySellTopCountAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForTopCount(topCount), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default)
    {
        using var operation = _pipeline.StartOperation("nbp.table.buy_sell_today");
        var traceSpan = operation.Span;

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellTodayAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).Today(), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("nbp.table.buy_sell_date");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate();

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDate(day), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var operation = _pipeline.StartOperation("nbp.table.buy_sell_range");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.from", fromDay.ToIso8601String());
        traceSpan.SetTag("nbp.to", toDay.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(to), toDay, WarsawCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator(nameof(from), nameof(to), (fromDay, toDay), DateRangeValidator.MaxTablesDateRange).Validate());

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDateRange(fromDay, toDay), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLastPublishedBeforeAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("nbp.table.buy_sell_last_published_before");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new LastPublishedBeforeDateValidator(nameof(date), day, CurrencyDateValidator.MinDate, WarsawCalendar.Today(_timeProvider)).Validate();

        return operation.Complete(await _pipeline.GetLastAsync(traceSpan, nameof(GetBuySellLastPublishedBeforeAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(NbpTable.C).ForDateRange(day.AddDays(-LastPublishedBeforeDateValidator.DailyWindowDays), day.AddDays(-1)), JsonContext.BuySellExchangeRateTableDtoArray, Mapper.MapLastToBuySellExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }
}
