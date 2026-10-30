using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve specific currency exchange rate data.
/// </summary>
/// <remarks>
/// <para>
/// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
/// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
/// </para>
/// <para>
/// Dates are <see cref="System.DateTime"/> on every target: only the date part of an argument is used (its time of day and
/// <see cref="System.DateTime.Kind"/> are ignored), and returned dates have the time 00:00 and <see cref="System.DateTimeKind.Unspecified"/>.
/// On .NET 8 and later, <c>NbpDateOnlyExtensions</c> adds <c>DateOnly</c> overloads.
/// </para>
/// <para>
/// Minor versions may add methods to this interface (ADR-0011). If you implement it yourself, e.g. as a decorator,
/// a new method breaks your build until you add it; a <c>throw new NotImplementedException();</c> stub is enough for the
/// methods your code doesn't call. Calling and mocking the interface aren't affected.
/// </para>
/// </remarks>
public interface INbpCurrencyExchangeRateClient
{
    /// <summary>
    /// Gets the latest mid exchange rate of the currency.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// Returns the most recently published value; until the next publication that is the previous one (for table B, usually last Wednesday's).
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="table"/> isn't a value defined by <see cref="TableType"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of mid exchange rates of the currency starting from the latest.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve, from 1 to 255</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rates or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="table"/> isn't a value defined by <see cref="TableType"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's mid exchange rate of the currency. If the exchange rate is not published yet for today, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="table"/> isn't a value defined by <see cref="TableType"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mid exchange rate of the currency for the specified date. If the exchange rate is not published for the given date, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date can't be lower than 2002-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="table"/> isn't a value defined by <see cref="TableType"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateTime date, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mid exchange rates of the currency for the specified date range. If the exchange rates are not published for the given date range, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date range can't exceed 367 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="table">Mid rate table that lists the currency: <see cref="TableType.A"/> (common currencies, default) or <see cref="TableType.B"/> (less common currencies)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rates or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="table"/> isn't a value defined by <see cref="TableType"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateTime from, DateTime to, TableType table = TableType.A, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest buy and sell exchange rate.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// Returns the most recently published value; until the next publication that is the previous one.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of buy and sell exchange rates starting from the latest.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve, from 1 to 255</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rates or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's buy and sell exchange rate. If the exchange rate is not published yet for today, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetBuySellLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the buy and sell exchange rate for the specified date. If the exchange rate is not published yet for the given date, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date can't be lower than 2002-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the buy and sell exchange rates for the specified date range. If the exchange rates are not published yet for the given date range, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date range can't exceed 367 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rates or an error</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>. Thrown before the task is returned.</exception>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
