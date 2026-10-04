using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Currency;

public partial class NbpCurrencyExchangeRateClient
{
    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestAsync(string currency, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_latest");
        traceSpan.SetTag("nbp.currency", currency);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellLatestAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).Latest(), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default)
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
    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayAsync(string currency, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_today");
        traceSpan.SetTag("nbp.currency", currency);

        var validation = new Iso4217Validator(nameof(currency), currency).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellTodayAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).Today(), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_date");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new CurrencyDateValidator(nameof(date), date, NbpCalendar.Today(_timeProvider)).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).ForDate(date), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.currency.buy_sell_range");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var validation = new Iso4217Validator(nameof(currency), currency).Validate()
            .And(new CurrencyDateValidator(nameof(to), to, NbpCalendar.Today(_timeProvider)).Validate())
            .And(new DateRangeValidator((from, to), DateRangeValidator.MaxRatesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetBuySellAsync), validation,
            () => _urlBuilderFactory.GetCurrencyBuilder(NbpTable.C, currency).ForDateRange(from, to), JsonContext.BuySellCurrencyExchangeRatesDto, Mapper.MapToBuySellExchangeRates, cancellationToken).ConfigureAwait(false);
    }
}
