using OpenUrzednik.Core;

namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Represents a client for interacting with the National Bank of Poland (NBP) API to retrieve specific currency exchange rate data.
/// </summary>
public interface INbpCurrencyExchangeRateClient
{
    /// <summary>
    /// Gets the latest currency exchange rate.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of currency exchange rates starting from the latest.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rates or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays currency exchange rate. Can return no data if the exchange rate is not published yet for today.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currency exchange rate for the specified date. Can return no data if the exchange rate is not published yet for the given date.
    /// Date can't be lower than 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rate or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currency exchange rates for the specified date range. Can return no data if the exchange rates are not published yet for the given date range.
    /// Date range can't exceed 93 days or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the currency exchange rates or an error</returns>
    Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest country exchange rate.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the country exchange rate or an error</returns>
    Task<OpenUrzednikResult<CountryExchangeRates>> GetCountryLatestAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of country exchange rates starting from the latest.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the country exchange rates or an error</returns>
    Task<OpenUrzednikResult<CountryExchangeRates>> GetCountryTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays country exchange rate. Can return no data if the exchange rate is not published yet for today.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the country exchange rate or an error</returns>
    Task<OpenUrzednikResult<CountryExchangeRates>> GetCountryTodayAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the country exchange rate for the specified date. Can return no data if the exchange rate is not published yet for the given date.
    /// Date can't be lower than 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the country exchange rate or an error</returns>
    Task<OpenUrzednikResult<CountryExchangeRates>> GetCountryAsync(string currency, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the country exchange rates for the specified date range. Can return no data if the exchange rates are not published yet for the given date range.
    /// Date range can't exceed 93 days or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the country exchange rates or an error</returns>
    Task<OpenUrzednikResult<CountryExchangeRates>> GetCountryAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest buy and sell exchange rate.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the series of buy and sell exchange rates starting from the latest.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="topCount">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rates or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the todays buy and sell exchange rate. Can return no data if the exchange rate is not published yet for today.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayAsync(string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the buy and sell exchange rate for the specified date. Can return no data if the exchange rate is not published yet for the given date.
    /// Date can't be lower than 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="date">Date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rate or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the buy and sell exchange rates for the specified date range. Can return no data if the exchange rates are not published yet for the given date range.
    /// Date range can't exceed 93 days or finish before 2002-01-02, because the NBP API doesn't support it.
    /// </summary>
    /// <param name="currency">ISO 4217 currency code</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the buy and sell exchange rates or an error</returns>
    Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
