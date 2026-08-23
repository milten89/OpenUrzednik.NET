using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve table of currency exchange rate data.
/// </summary>
public interface INbpExchangeRateTableClient
{
    /// <summary>
    /// Gets the latest table of currency exchange rate for the specified table type.
    /// </summary>
    /// <param name="table">Table type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(TableType table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of the table of currency exchange rate for the specified table type starting from the latest.
    /// </summary>
    /// <param name="table">Table type</param>
    /// <param name="topCount">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(TableType table, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays table of currency exchange rate for the specified table type. Can return no data if the table is not published yet for today.
    /// </summary>
    /// <param name="table">Table type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(TableType table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of currency exchange rate for the specified table type and date. Can return no data if the table is not published yet for the given date.
    /// Date can't be lower than 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="table">Table type</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(TableType table, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of currency exchange rate for the specified table type and date range. Can return no data if the tables are not published yet for the given date range.
    /// Date range can't exceed 93 days or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="table">Table type</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(TableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest table of buy and sell currency exchange rate.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of table of buy and sell currency exchange rate starting from the latest.
    /// </summary>
    /// <param name="topCount">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays table of buy and sell currency exchange rate. Can return no data if the table is not published yet for today.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of buy and sell currency exchange rate for the specified date. Can return no data if the table is not published yet for the given date.
    /// Date can't be lower than 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the table of buy and sell currency exchange rate for the specified date range. Can return no data if the tables are not published yet for the given date range.
    /// Date range can't exceed 93 days or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the table of buy and sell currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
