using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class NbpExchangeRateTableClient
{
    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(TableType table, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.latest");
        traceSpan.SetTag("nbp.table", table);

        var validation = new TableTypeValidator(nameof(table), table).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetLatestAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).Latest(), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(TableType table, int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.top_count");
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TableTypeValidator(nameof(table), table).Validate()
            .And(new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxForTable(table)).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(TableType table, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.today");
        traceSpan.SetTag("nbp.table", table);

        var validation = new TableTypeValidator(nameof(table), table).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetTodayAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).Today(), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(TableType table, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.date");
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var validation = new TableTypeValidator(nameof(table), table).Validate()
            .And(new CurrencyDateValidator(nameof(date), date, NbpCalendar.Today(_timeProvider)).Validate());

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDate(date), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(TableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.table.range");
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var validation = new TableTypeValidator(nameof(table), table).Validate()
            .And(new CurrencyDateValidator(nameof(to), to, NbpCalendar.Today(_timeProvider)).Validate())
            .And(new DateRangeValidator((from, to), DateRangeValidator.MaxTablesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDateRange(from, to), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false);
    }
}
