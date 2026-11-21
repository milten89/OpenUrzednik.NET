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

        return operation.Complete(await _pipeline.GetOrNotOnListAsync(traceSpan, nameof(SearchByRegonAsync), validation,
            () => $"search/regon/{validator.Normalized}?date={dayText}", "search/regon/{regon}",
            WhiteListJsonContext.Default.EntityResponseDto, Mapper.MapToSubjectSearchResult, cancellationToken).ConfigureAwait(false));
    }
}
