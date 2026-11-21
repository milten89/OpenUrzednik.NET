using OpenUrzednik.Core;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

public sealed partial class NbpExchangeRateTableClient
{
    /// <inheritdoc/>
    public Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(TableType table, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetLatestCoreAsync(table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestCoreAsync(TableType table, CancellationToken cancellationToken)
    {
        using var operation = _pipeline.StartOperation("nbp.table.latest");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.table", table.ToTagValue());

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetLatestAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).Latest(), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(TableType table, int topCount, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetTopCountCoreAsync(table, topCount, cancellationToken);
    }

    private async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountCoreAsync(TableType table, int topCount, CancellationToken cancellationToken)
    {
        using var operation = _pipeline.StartOperation("nbp.table.top_count");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxForTable(table)).Validate();

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(TableType table, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetTodayCoreAsync(table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayCoreAsync(TableType table, CancellationToken cancellationToken)
    {
        using var operation = _pipeline.StartOperation("nbp.table.today");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.table", table.ToTagValue());

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetTodayAsync), OpenUrzednikResult.Success(),
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).Today(), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false));
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
        using var operation = _pipeline.StartOperation("nbp.table.date");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTagIfRecording("nbp.date", day, static d => d.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate();

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDate(day), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false));
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
        using var operation = _pipeline.StartOperation("nbp.table.range");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTagIfRecording("nbp.from", fromDay, static d => d.ToIso8601String());
        traceSpan.SetTagIfRecording("nbp.to", toDay, static d => d.ToIso8601String());

        var validation = new CurrencyDateValidator(nameof(to), toDay, WarsawCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator(nameof(from), nameof(to), (fromDay, toDay), DateRangeValidator.MaxTablesDateRange).Validate());

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDateRange(fromDay, toDay), JsonContext.ExchangeRateTableDtoArray, Mapper.MapToExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<ExchangeRateTable>> GetLastPublishedBeforeAsync(TableType table, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentGuard.ThrowIfUndefined(table);

        return GetLastPublishedBeforeCoreAsync(table, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<ExchangeRateTable>> GetLastPublishedBeforeCoreAsync(TableType table, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("nbp.table.last_published_before");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTagIfRecording("nbp.date", day, static d => d.ToIso8601String());

        var validation = new LastPublishedBeforeDateValidator(nameof(date), day, CurrencyDateValidator.MinDate, WarsawCalendar.Today(_timeProvider)).Validate();

        return operation.Complete(await _pipeline.GetLastAsync(traceSpan, nameof(GetLastPublishedBeforeAsync), validation,
            () => _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table)).ForDateRange(day.AddDays(-LastPublishedBeforeDateValidator.WindowForTable(table)), day.AddDays(-1)), JsonContext.ExchangeRateTableDtoArray, Mapper.MapLastToExchangeRateTable, cancellationToken).ConfigureAwait(false));
    }
}
