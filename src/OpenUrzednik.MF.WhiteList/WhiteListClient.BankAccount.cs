using OpenUrzednik.Core;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Identifiers;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.MF.WhiteList.Validation;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.MF.WhiteList;

public sealed partial class WhiteListClient
{
    // Stryker disable once String : false survivor, Stryker can't activate a static field initializer; an empty name makes the element names "[0]", "[1]", …, which fails the tests of invalid identifiers (checked by hand)
    private static readonly string[] BankAccountsElementNames = WhiteListBatchValidator.ElementNames("bankAccounts");

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<SubjectsSearchResult>> SearchByBankAccountAsync(string bankAccount, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bankAccount);

        return SearchByBankAccountCoreAsync(bankAccount, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<SubjectsSearchResult>> SearchByBankAccountCoreAsync(string bankAccount, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("mf_white_list.search.bank_account");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("mf_white_list.bank_account", bankAccount, static v => SensitiveValue.Mask(v));
        var dayText = day.ToIso8601String();
        traceSpan.SetTag("mf_white_list.date", dayText);

        var validator = new BankAccountNumberValidator(nameof(bankAccount), bankAccount);
        var validation = validator.Validate()
            .And(new WhiteListDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate());

        return operation.Complete(await _pipeline.GetAnswerAsync(traceSpan, nameof(SearchByBankAccountAsync), validation,
            () => $"search/bank-account/{validator.Normalized}?date={dayText}", "search/bank-account/{account}",
            WhiteListJsonContext.Default.EntityListResponseDto, Mapper.MapToSubjectsSearchResult, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BatchSearchResult>> SearchByBankAccountsAsync(IReadOnlyList<string> bankAccounts, DateTime date, CancellationToken cancellationToken = default)
    {
        WhiteListBatchValidator.ThrowIfAnyNull(bankAccounts, nameof(bankAccounts));

        return SearchBatchCoreAsync("mf_white_list.search.bank_accounts", nameof(SearchByBankAccountsAsync),
            new WhiteListBatchValidator(nameof(bankAccounts), bankAccounts, BankAccountsElementNames, static (name, value) => new BankAccountNumberValidator(name, value)),
            date, "search/bank-accounts/", "search/bank-accounts/{accounts}", cancellationToken);
    }
}
