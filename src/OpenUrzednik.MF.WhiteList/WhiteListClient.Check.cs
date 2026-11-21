using OpenUrzednik.Core;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Identifiers;
using OpenUrzednik.MF.WhiteList.Check;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Validation;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.MF.WhiteList;

public sealed partial class WhiteListClient
{
    /// <inheritdoc/>
    public Task<OpenUrzednikResult<AccountCheckResult>> CheckNipBankAccountAsync(string nip, string bankAccount, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nip);
        ArgumentNullException.ThrowIfNull(bankAccount);

        return CheckNipBankAccountCoreAsync(nip, bankAccount, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<AccountCheckResult>> CheckNipBankAccountCoreAsync(string nip, string bankAccount, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("mf_white_list.check.nip_bank_account");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("mf_white_list.nip", nip, static v => SensitiveValue.Mask(v));
        traceSpan.SetTagIfRecording("mf_white_list.bank_account", bankAccount, static v => SensitiveValue.Mask(v));
        var dayText = day.ToIso8601String();
        traceSpan.SetTag("mf_white_list.date", dayText);

        var nipValidator = new NipValidator(nameof(nip), nip);
        var accountValidator = new BankAccountNumberValidator(nameof(bankAccount), bankAccount);
        var validation = nipValidator.Validate()
            .And(accountValidator.Validate())
            .And(new WhiteListDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate());

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(CheckNipBankAccountAsync), validation,
            () => $"check/nip/{nipValidator.Normalized}/bank-account/{accountValidator.Normalized}?date={dayText}", "check/nip/{nip}/bank-account/{account}",
            WhiteListJsonContext.Default.EntityCheckResponseDto, Mapper.MapToAccountCheckResult, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<AccountCheckResult>> CheckRegonBankAccountAsync(string regon, string bankAccount, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(regon);
        ArgumentNullException.ThrowIfNull(bankAccount);

        return CheckRegonBankAccountCoreAsync(regon, bankAccount, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<AccountCheckResult>> CheckRegonBankAccountCoreAsync(string regon, string bankAccount, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("mf_white_list.check.regon_bank_account");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("mf_white_list.regon", regon, static v => SensitiveValue.Mask(v));
        traceSpan.SetTagIfRecording("mf_white_list.bank_account", bankAccount, static v => SensitiveValue.Mask(v));
        var dayText = day.ToIso8601String();
        traceSpan.SetTag("mf_white_list.date", dayText);

        var regonValidator = new RegonValidator(nameof(regon), regon);
        var accountValidator = new BankAccountNumberValidator(nameof(bankAccount), bankAccount);
        var validation = regonValidator.Validate()
            .And(accountValidator.Validate())
            .And(new WhiteListDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate());

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(CheckRegonBankAccountAsync), validation,
            () => $"check/regon/{regonValidator.Normalized}/bank-account/{accountValidator.Normalized}?date={dayText}", "check/regon/{regon}/bank-account/{account}",
            WhiteListJsonContext.Default.EntityCheckResponseDto, Mapper.MapToAccountCheckResult, cancellationToken).ConfigureAwait(false));
    }
}
