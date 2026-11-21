#if NET
using OpenUrzednik.Core;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// <c>DateOnly</c> overloads of the MF White List clients' date methods, on .NET 8 and later. They call the <see cref="DateTime"/> methods,
/// which every target has, so the interfaces stay the same in every build.
/// </summary>
public static class WhiteListDateOnlyExtensions
{
    /// <inheritdoc cref="IWhiteListClient.SearchByNipAsync(string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="nip"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<SubjectSearchResult>> SearchByNipAsync(this IWhiteListClient client, string nip, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SearchByNipAsync(nip, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.SearchByRegonAsync(string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="regon"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<SubjectSearchResult>> SearchByRegonAsync(this IWhiteListClient client, string regon, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SearchByRegonAsync(regon, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.SearchByBankAccountAsync(string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="bankAccount"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<SubjectsSearchResult>> SearchByBankAccountAsync(this IWhiteListClient client, string bankAccount, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SearchByBankAccountAsync(bankAccount, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.SearchByNipsAsync(IReadOnlyList{string}, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="nips"/> or one of its elements is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<BatchSearchResult>> SearchByNipsAsync(this IWhiteListClient client, IReadOnlyList<string> nips, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SearchByNipsAsync(nips, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.SearchByRegonsAsync(IReadOnlyList{string}, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="regons"/> or one of its elements is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<BatchSearchResult>> SearchByRegonsAsync(this IWhiteListClient client, IReadOnlyList<string> regons, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SearchByRegonsAsync(regons, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.SearchByBankAccountsAsync(IReadOnlyList{string}, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="bankAccounts"/> or one of its elements is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<BatchSearchResult>> SearchByBankAccountsAsync(this IWhiteListClient client, IReadOnlyList<string> bankAccounts, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SearchByBankAccountsAsync(bankAccounts, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.CheckNipBankAccountAsync(string, string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="nip"/> or <paramref name="bankAccount"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<AccountCheckResult>> CheckNipBankAccountAsync(this IWhiteListClient client, string nip, string bankAccount, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.CheckNipBankAccountAsync(nip, bankAccount, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListClient.CheckRegonBankAccountAsync(string, string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/>, <paramref name="regon"/> or <paramref name="bankAccount"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<AccountCheckResult>> CheckRegonBankAccountAsync(this IWhiteListClient client, string regon, string bankAccount, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.CheckRegonBankAccountAsync(regon, bankAccount, date.ToPublicDate(), cancellationToken);
    }

    /// <inheritdoc cref="IWhiteListStatusClient.GetVatStatusByNipAsync(string, DateTime, CancellationToken)"/>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="nip"/> is <see langword="null"/>.</exception>
    public static Task<OpenUrzednikResult<VatStatusResult>> GetVatStatusByNipAsync(this IWhiteListStatusClient client, string nip, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.GetVatStatusByNipAsync(nip, date.ToPublicDate(), cancellationToken);
    }
}
#endif
