using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

public partial class NbpCurrencyExchangeRateClient
{
    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestAsync(string currency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return GetBuySellLatestCoreAsync(currency, cancellationToken);
    }

    private async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestCoreAsync(string currency, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_latest");
        traceSpan.SetTag("nbp.currency", currency);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellLatestAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).Latest(), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return GetBuySellTopCountCoreAsync(currency, topCount, cancellationToken);
    }

    private async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountCoreAsync(string currency, int topCount, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_top_count");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxTopCount).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellTopCountAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).ForTopCount(topCount), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayAsync(string currency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return GetBuySellTodayCoreAsync(currency, cancellationToken);
    }

    private async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayCoreAsync(string currency, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_today");
        traceSpan.SetTag("nbp.currency", currency);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellTodayAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).Today(), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return GetBuySellCoreAsync(currency, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellCoreAsync(string currency, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_date");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new CurrencyDateValidator(nameof(date), day, NbpCalendar.Today(_timeProvider)).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).ForDate(day), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return GetBuySellCoreAsync(currency, from, to, cancellationToken);
    }

    private async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellCoreAsync(string currency, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_range");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.from", fromDay.ToIso8601String());
        traceSpan.SetTag("nbp.to", toDay.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new CurrencyDateValidator(nameof(to), toDay, NbpCalendar.Today(_timeProvider)).Validate())
            .And(new DateRangeValidator((fromDay, toDay), DateRangeValidator.MaxRatesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).ForDateRange(fromDay, toDay), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }
}
