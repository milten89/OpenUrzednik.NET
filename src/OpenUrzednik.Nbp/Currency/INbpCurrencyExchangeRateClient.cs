using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve specific currency exchange rate data.
/// </summary>
/// <remarks>
/// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
/// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
/// </remarks>
public interface INbpCurrencyExchangeRateClient
{
    /// <summary>
    /// Gets the latest mid exchange rate of the currency.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// Returns the most recently published value, so before today's publication it is the previous one.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of mid exchange rates of the currency starting from the latest.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve, from 1 to 255</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rates or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's mid exchange rate of the currency. Can return no data if the exchange rate is not published yet for today.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mid exchange rate of the currency for the specified date. Can return no data if the exchange rate is not published for the given date.
    /// Date can't be lower than 2002-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mid exchange rates of the currency for the specified date range. Can return no data if the exchange rates are not published for the given date range.
    /// Date range can't exceed 367 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rates or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest buy and sell exchange rate.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// Returns the most recently published value, so before today's publication it is the previous one.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of buy and sell exchange rates starting from the latest.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve, from 1 to 255</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rates or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays buy and sell exchange rate. Can return no data if the exchange rate is not published yet for today.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetBuySellLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the buy and sell exchange rate for the specified date. Can return no data if the exchange rate is not published yet for the given date.
    /// Date can't be lower than 2002-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the buy and sell exchange rates for the specified date range. Can return no data if the exchange rates are not published yet for the given date range.
    /// Date range can't exceed 367 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rates or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
