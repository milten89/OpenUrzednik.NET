using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.Gold;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve gold prices.
/// </summary>
public interface INbpGoldPriceClient
{
    /// <summary>
    /// Gets the latest gold price.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold price or an error</returns>
    Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of gold prices starting from the latest.
    /// </summary>
    /// <param name="topCount">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold prices or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays gold price. Can return no data if the gold price is not published yet for today.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold price or an error</returns>
    Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the gold price for the specified date. Can return no data if the gold price is not published yet for the given date.
    /// Date can't be lower than 2013-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold price or an error</returns>
    Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the gold price for the specified date range. Can return no data if the gold price are not published yet for the given date range.
    /// Date range can't exceed 93 days or finish before 2013-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold prices or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
