using System.Collections.Concurrent;
using System.Diagnostics;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Diagnostics;

/// <summary>
/// An <see cref="IOpenUrzednikTraceSource"/> that starts <see cref="Activity"/>s on an <see cref="ActivitySource"/>.
/// </summary>
/// <remarks>
/// <para>
/// Providers use the source name <c>OpenUrzednik.&lt;Provider&gt;</c> (e.g. <c>OpenUrzednik.Nbp</c>), so OpenTelemetry users
/// subscribe with <c>AddSource("OpenUrzednik.*")</c>. When nothing listens to the source, or the span isn't sampled,
/// <see cref="StartSpan"/> returns a no-op span and allocates nothing.
/// </para>
/// <para>
/// Every span is <see cref="ActivityKind.Internal"/>, including <c>&lt;provider&gt;.http.get</c>: the client span of the
/// request itself comes from <see cref="HttpClient"/>'s own instrumentation, nested under it.
/// </para>
/// </remarks>
public sealed class ActivityTraceSource : IOpenUrzednikTraceSource
{
    // Lazy, so a race in GetOrAdd can't create (and leave registered) a second source with the same name.
    private static readonly ConcurrentDictionary<string, Lazy<ActivityTraceSource>> Shared = new(StringComparer.Ordinal);

    private readonly ActivitySource _source;

    /// <summary>Creates an adapter over <paramref name="source"/>, which the caller owns and disposes.</summary>
    /// <param name="source">The source the spans are started on.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public ActivityTraceSource(ActivitySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>The underlying source.</summary>
    public ActivitySource Source => _source;

    /// <summary>
    /// Returns the adapter for the source named <paramref name="name"/>, creating the source on first use.
    /// The source lives as long as the process, as <see cref="ActivitySource"/>s usually do.
    /// </summary>
    /// <param name="name">The source name, e.g. <c>OpenUrzednik.Nbp</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or white space.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    public static ActivityTraceSource GetShared(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Shared.GetOrAdd(name, static key => new Lazy<ActivityTraceSource>(() => new ActivityTraceSource(new ActivitySource(key)))).Value;
    }

    /// <inheritdoc/>
    public IOpenUrzednikSpan StartSpan(string operationName)
    {
        var activity = _source.StartActivity(operationName, ActivityKind.Internal);
        return activity is null ? NullOpenUrzednikTraceSource.Instance.StartSpan(operationName) : new ActivitySpan(activity);
    }
}
