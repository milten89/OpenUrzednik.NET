using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve table of currency exchange rate data.
/// </summary>
/// <remarks>
/// <para>
/// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
/// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
/// </para>
/// <para>
/// Dates are <c>DateOnly</c> on .NET and <see cref="System.DateTime"/> on netstandard2.0 (.NET Framework), where only the date part
/// is used and returned dates have the time 00:00 and <see cref="System.DateTimeKind.Unspecified"/>.
/// </para>
/// </remarks>
public interface INbpExchangeRateTableClient
{
    /// <summary>
    /// Gets the latest table of currency exchange rate for the specified table type.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// Returns the most recently published value; until the next publication that is the previous one (for table B, usually last Wednesday's).
    /// </remarks>
    /// <param name="table">Table type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(TableType table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of the table of currency exchange rate for the specified table type starting from the latest.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// </remarks>
    /// <param name="table">Table type</param>
    /// <param name="topCount">Number of tables to retrieve: 1 to 67 for table A, 1 to 14 for table B; the API rejects larger counts.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(TableType table, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's table of currency exchange rate for the specified table type. If the table is not published yet for today, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="table">Table type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(TableType table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of currency exchange rate for the specified table type and date. If the table is not published yet for the given date, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date can't be lower than 2002-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// </remarks>
    /// <param name="table">Table type</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(TableType table, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of currency exchange rate for the specified table type and date range. If the tables are not published yet for the given date range, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date range can't exceed 93 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table A (common currencies) is published on business days between 11:45 and 12:15, table B (less common currencies) on Wednesdays between 11:45 and 12:15, or on the previous business day when Wednesday is a holiday (Europe/Warsaw time).
    /// </remarks>
    /// <param name="table">Table type</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(TableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest table of buy and sell currency exchange rate.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// Returns the most recently published value; until the next publication that is the previous one (for table B, usually last Wednesday's).
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of table of buy and sell currency exchange rate starting from the latest.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// </remarks>
    /// <param name="topCount">Number of tables to retrieve, from 1 to 67; the API rejects larger counts.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's table of buy and sell currency exchange rate. If the table is not published yet for today, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetBuySellLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of buy and sell currency exchange rate for the specified date. If the table is not published yet for the given date, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date can't be lower than 2002-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// </remarks>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of buy and sell currency exchange rate for the specified date range. If the tables are not published yet for the given date range, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date range can't exceed 93 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Table C (buy and sell rates) is published on business days between 7:45 and 8:15 (Europe/Warsaw time).
    /// </remarks>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
