using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Currency;

public partial class NbpCurrencyExchangeRateClient
{
    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.latest");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TableTypeValidator(nameof(table), table).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetLatestAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).Latest(), JsonContext.CurrencyExchangeRatesDto, Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.top_count");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TableTypeValidator(nameof(table), table).Validate())
            .And(new TopCountValidator(nameof(topCount), topCount).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).ForTopCount(topCount), JsonContext.CurrencyExchangeRatesDto, Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.today");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TableTypeValidator(nameof(table), table).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetTodayAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).Today(), JsonContext.CurrencyExchangeRatesDto, Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.date");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TableTypeValidator(nameof(table), table).Validate())
            .And(new CurrencyDateValidator(nameof(date), date, NbpCalendar.Today(_timeProvider)).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).ForDate(date), JsonContext.CurrencyExchangeRatesDto, Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.range");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TableTypeValidator(nameof(table), table).Validate())
            .And(new CurrencyDateValidator(nameof(to), to, NbpCalendar.Today(_timeProvider)).Validate())
            .And(new DateRangeValidator((from, to), DateRangeValidator.MaxRatesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency).ForDateRange(from, to), JsonContext.CurrencyExchangeRatesDto, Mapper.MapToCurrencyExchangeRates, cancellationToken).ConfigureAwait(false);
    }
}
