#if NET
using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp;

/// <summary>
/// <c>DateOnly</c> overloads of the NBP clients' date methods, on .NET 8 and later. They call the <see cref="DateTime"/> methods,
/// which every target has, so the interfaces stay the same in every build.
/// </summary>
public static class NbpDateOnlyExtensions
{
    /// <inheritdoc cref="INbpGoldPriceClient.GetAsync(DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<GoldPrice>> GetAsync(this INbpGoldPriceClient client, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpGoldPriceClient.GetAsync(DateTime, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(this INbpGoldPriceClient client, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(from.ToPublicDate(), to.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpCurrencyExchangeRateClient.GetAsync(string, DateTime, TableType, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="currency"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(this INbpCurrencyExchangeRateClient client, string currency, DateOnly date,
        TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(currency, date.ToPublicDate(), table, cancellationToken);
    }

    /// <inheritdoc cref="INbpCurrencyExchangeRateClient.GetAsync(string, DateTime, DateTime, TableType, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="currency"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(this INbpCurrencyExchangeRateClient client, string currency, DateOnly from, DateOnly to,
        TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(currency, from.ToPublicDate(), to.ToPublicDate(), table, cancellationToken);
    }

    /// <inheritdoc cref="INbpCurrencyExchangeRateClient.GetBuySellAsync(string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="currency"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(this INbpCurrencyExchangeRateClient client, string currency, DateOnly date,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetBuySellAsync(currency, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpCurrencyExchangeRateClient.GetBuySellAsync(string, DateTime, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="currency"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(this INbpCurrencyExchangeRateClient client, string currency, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetBuySellAsync(currency, from.ToPublicDate(), to.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpExchangeRateTableClient.GetAsync(TableType, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(this INbpExchangeRateTableClient client, TableType table, DateOnly date,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(table, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpExchangeRateTableClient.GetAsync(TableType, DateTime, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(this INbpExchangeRateTableClient client, TableType table, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetAsync(table, from.ToPublicDate(), to.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpExchangeRateTableClient.GetBuySellAsync(DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(this INbpExchangeRateTableClient client, DateOnly date,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetBuySellAsync(date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="INbpExchangeRateTableClient.GetBuySellAsync(DateTime, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(this INbpExchangeRateTableClient client, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetBuySellAsync(from.ToPublicDate(), to.ToPublicDate(), cancellationToken);
    }
}
#endif
