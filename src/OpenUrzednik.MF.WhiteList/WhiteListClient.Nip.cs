using OpenUrzednik.Core;
using OpenUrzednik.Identifiers;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.MF.WhiteList.Validation;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.MF.WhiteList;

public sealed partial class WhiteListClient
{
    /// <inheritdoc/>
    public Task<OpenUrzednikResult<SubjectSearchResult>> SearchByNipAsync(string nip, DateTime date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nip);

        return SearchByNipCoreAsync(nip, date, cancellationToken);
    }

    private async Task<OpenUrzednikResult<SubjectSearchResult>> SearchByNipCoreAsync(string nip, DateTime date, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("mf_white_list.search.nip");
        var traceSpan = operation.Span;
        traceSpan.SetTag("mf_white_list.nip", SensitiveValue.Mask(nip));
        var dayText = day.ToIso8601String();
        traceSpan.SetTag("mf_white_list.date", dayText);

        var validator = new NipValidator(nameof(nip), nip);
        var validation = validator.Validate()
            .And(new WhiteListDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate());

        return operation.Complete(await _pipeline.GetOrNotOnListAsync(traceSpan, nameof(SearchByNipAsync), validation,
            () => $"search/nip/{validator.Normalized}?date={dayText}", "search/nip/{nip}",
            WhiteListJsonContext.Default.EntityResponseDto, Mapper.MapToSubjectSearchResult, cancellationToken).ConfigureAwait(false));
    }
}
