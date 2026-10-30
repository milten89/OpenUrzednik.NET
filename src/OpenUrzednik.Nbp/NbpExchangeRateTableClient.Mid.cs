using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

public partial class NbpExchangeRateTableClient
{
    /// <inheritdoc/>
    public Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(TableType table, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetLatestCoreAsync(table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestCoreAsync(TableType table, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.latest");
        traceSpan.SetTag("nbp.table", table.ToTagValue());

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetLatestAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).Latest(), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(TableType table, int topCount, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetTopCountCoreAsync(table, topCount, cancellationToken);
    }

    private async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountCoreAsync(TableType table, int topCount, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.top_count");
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxForTable(table)).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(TableType table, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetTodayCoreAsync(table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayCoreAsync(TableType table, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.today");
        traceSpan.SetTag("nbp.table", table.ToTagValue());

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetTodayAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).Today(), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(TableType table, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetCoreAsync(table, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<ExchangeRateTable>> GetCoreAsync(TableType table, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.date");
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(date), day, NbpCalendar.Today(_timeProvider)).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDate(day), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(TableType table, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetCoreAsync(table, from, to, cancellationToken);
    }

    private async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetCoreAsync(TableType table, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.range");
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.from", fromDay.ToIso8601String());
        traceSpan.SetTag("nbp.to", toDay.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(to), toDay, NbpCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator((fromDay, toDay), DateRangeValidator.MaxTablesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDateRange(fromDay, toDay), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }
}
