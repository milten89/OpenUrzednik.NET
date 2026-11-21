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
    private static readonly string[] RegonsElementNames = WhiteListBatchValidator.ElementNames("regons");

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<SubjectSearchResult>> SearchByRegonAsync(string regon, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(regon);

        return SearchByRegonCoreAsync(regon, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<SubjectSearchResult>> SearchByRegonCoreAsync(string regon, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("mf_white_list.search.regon");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("mf_white_list.regon", regon, static v => SensitiveValue.Mask(v));
        var dayText = day.ToIso8601String();
        traceSpan.SetTag("mf_white_list.date", dayText);

        var validator = new RegonValidator(nameof(regon), regon);
        var validation = validator.Validate()
            .And(new WhiteListDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate());

        return operation.Complete(await _pipeline.GetAnswerAsync(traceSpan, nameof(SearchByRegonAsync), validation,
            () => $"search/regon/{validator.Normalized}?date={dayText}", "search/regon/{regon}",
            WhiteListJsonContext.Default.EntityResponseDto, Mapper.MapToSubjectSearchResult, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<OpenUrzednikResult<BatchSearchResult>> SearchByRegonsAsync(IReadOnlyList<string> regons, DateTime date, CancellationToken cancellationToken = default)
    {
        WhiteListBatchValidator.ThrowIfAnyNull(regons, nameof(regons));

        return SearchBatchCoreAsync("mf_white_list.search.regons", nameof(SearchByRegonsAsync),
            new WhiteListBatchValidator(nameof(regons), regons, RegonsElementNames, static (name, value) => new RegonValidator(name, value)),
            date, "search/regons/", "search/regons/{regons}", cancellationToken);
    }
}
