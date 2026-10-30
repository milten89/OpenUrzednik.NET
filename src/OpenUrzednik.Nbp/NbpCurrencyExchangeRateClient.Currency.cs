using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

public sealed partial class NbpCurrencyExchangeRateClient
{
    /// <inheritdoc/>
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentGuard.ThrowIfUndefined(table);

        return GetLatestCoreAsync(currency, table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestCoreAsync(string currency, TableType table, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.latest");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table.ToTagValue());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetLatestAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).Latest(), JsonContext.CurrencyExchangeRatesDto, Currency.Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentGuard.ThrowIfUndefined(table);

        return GetTopCountCoreAsync(currency, topCount, table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountCoreAsync(string currency, int topCount, TableType table, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.top_count");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxTopCount).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).ForTopCount(topCount), JsonContext.CurrencyExchangeRatesDto, Currency.Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentGuard.ThrowIfUndefined(table);

        return GetTodayCoreAsync(currency, table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayCoreAsync(string currency, TableType table, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.today");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table.ToTagValue());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetTodayAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).Today(), JsonContext.CurrencyExchangeRatesDto, Currency.Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateTime date, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentGuard.ThrowIfUndefined(table);

        return GetCoreAsync(currency, date, table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetCoreAsync(string currency, DateTime date, TableType table, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.date");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new CurrencyDateValidator(nameof(date), day, NbpCalendar.Today(_timeProvider)).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).ForDate(day), JsonContext.CurrencyExchangeRatesDto, Currency.Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateTime from, DateTime to, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentGuard.ThrowIfUndefined(table);

        return GetCoreAsync(currency, from, to, table, cancellationToken);
    }

    private async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetCoreAsync(string currency, DateTime from, DateTime to, TableType table, CancellationToken cancellationToken)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.range");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table.ToTagValue());
        traceSpan.SetTag("nbp.from", fromDay.ToIso8601String());
        traceSpan.SetTag("nbp.to", toDay.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new CurrencyDateValidator(nameof(to), toDay, NbpCalendar.Today(_timeProvider)).Validate())
            .And(new DateRangeValidator((fromDay, toDay), DateRangeValidator.MaxRatesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).ForDateRange(fromDay, toDay), JsonContext.CurrencyExchangeRatesDto, Currency.Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }
}
