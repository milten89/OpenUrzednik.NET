using System.Collections.Concurrent;
using System.Diagnostics;

namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// One <see cref="ActivitySource"/> per name for the whole process, as an <see cref="IOpenUrzednikTraceSource"/>.
/// On the .NET targets a provider client without a trace source uses it (<c>OpenUrzednik.&lt;Provider&gt;</c>), so
/// <c>AddSource("OpenUrzednik.*")</c> is enough without DI; <c>ActivityTraceSource.GetShared</c> in <c>OpenUrzednik.Diagnostics</c>
/// returns the same source. Compiled into Core on the .NET targets and linked into <c>OpenUrzednik.Diagnostics</c> for netstandard2.0 (ADR-0003).
/// </summary>
/// <remarks>
/// On .NET, OpenUrzednik.Diagnostics and the providers call <see cref="Get"/> and <see cref="Source"/> through <c>InternalsVisibleTo</c>,
/// and an app may combine a newer Core with older packages: within a major version add members, never change or remove these.
/// </remarks>
internal sealed class SharedActivityTraceSource : IOpenUrzednikTraceSource
{
    // Lazy, so a race in GetOrAdd can't create (and leave registered) a second source with the same name.
    private static readonly ConcurrentDictionary<string, Lazy<SharedActivityTraceSource>> Shared = new(StringComparer.Ordinal);

    private SharedActivityTraceSource(ActivitySource source) => Source = source;

    /// <summary>The source; it lives as long as the process.</summary>
    internal ActivitySource Source { get; }

    /// <summary>Returns the trace source for <paramref name="name"/>, creating its <see cref="ActivitySource"/> on first use.</summary>
    internal static SharedActivityTraceSource Get(string name)
        => Shared.GetOrAdd(name, static key => new Lazy<SharedActivityTraceSource>(() => new SharedActivityTraceSource(new ActivitySource(key)))).Value;

    /// <inheritdoc/>
    public IOpenUrzednikSpan StartSpan(string operationName) => ActivitySpan.Start(Source, operationName);
}
