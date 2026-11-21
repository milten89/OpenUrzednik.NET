using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Span tags whose values cost an allocation to build (a formatted date, a masked identifier): the value is built only while the
/// span is recording, so a call that nobody traces allocates nothing for it (#345). Pass a <see langword="static"/> lambda, which the
/// compiler caches, so the call itself allocates nothing either:
/// <code>
/// traceSpan.SetTagIfRecording("nbp.date", day, static d => d.ToIso8601String());
/// </code>
/// </summary>
internal static class SpanTags
{
    /// <summary>
    /// Sets the tag <paramref name="key"/> to <paramref name="value"/> of <paramref name="state"/> when the span is recording;
    /// otherwise does nothing and doesn't call <paramref name="value"/>.
    /// </summary>
    /// <param name="span">Span to tag.</param>
    /// <param name="key">Tag name.</param>
    /// <param name="state">What the value is built from.</param>
    /// <param name="value">Builds the value; called at most once.</param>
    internal static void SetTagIfRecording<TState>(this IOpenUrzednikSpan span, string key, TState state, Func<TState, string> value)
    {
        if (span.IsRecording)
            span.SetTag(key, value(state));
    }
}
