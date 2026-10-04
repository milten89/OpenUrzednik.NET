using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.Gold;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve gold prices.
/// </summary>
/// <remarks>
/// <para>
/// Gold prices are published on business days in Poland.
/// </para>
/// <para>
/// Dates are <c>DateOnly</c> on .NET and <see cref="System.DateTime"/> on netstandard2.0 (.NET Framework), where only the date part
/// is used and returned dates have the time 00:00 and <see cref="System.DateTimeKind.Unspecified"/>.
/// </para>
/// </remarks>
public interface INbpGoldPriceClient
{
    /// <summary>
    /// Gets the latest gold price.
    /// </summary>
    /// <remarks>
    /// Gold prices are published on business days in Poland.
    /// Returns the most recently published value; until the next publication that is the previous one.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold price or an error</returns>
    Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of gold prices starting from the latest.
    /// </summary>
    /// <remarks>
    /// Gold prices are published on business days in Poland.
    /// </remarks>
    /// <param name="topCount">Number of records to retrieve, from 1 to 255</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold prices or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's gold price. If the gold price is not published yet for today, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// </summary>
    /// <remarks>
    /// Gold prices are published on business days in Poland.
    /// Until today's publication, and on days without one, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>; use <see cref="GetLatestAsync"/> to get the most recent published value.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold price or an error</returns>
    Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the gold price for the specified date. If the gold price is not published yet for the given date, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date can't be lower than 2013-01-02 or later than today (Europe/Warsaw date), because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Gold prices are published on business days in Poland.
    /// </remarks>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold price or an error</returns>
    Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the gold price for the specified date range. If the gold prices are not published yet for the given date range, the result is a <see cref="OpenUrzednik.Core.Errors.NotFoundError"/>.
    /// Date range can't exceed 367 days (<c>to - from</c>), start after the end, end in the future (Europe/Warsaw date) or finish before 2013-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <remarks>
    /// Gold prices are published on business days in Poland.
    /// </remarks>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the gold prices or an error</returns>
    Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
