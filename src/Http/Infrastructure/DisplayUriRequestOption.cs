using System.Diagnostics.CodeAnalysis;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Carries the display URI of a request whose path or query holds personal identifiers (ADR-0019) from the executor to the
/// DI package's HttpClient logger, which writes it instead of the request URI (ADR-0017). The DI package compiles this file too.
/// </summary>
/// <remarks>
/// The key is a contract between packages: an app may pair a provider with an older or newer DI package, and a logger that doesn't
/// find the key logs the real path. Within a major, never rename or remove it (like Core's cross-package internals, ADR-0003).
/// </remarks>
internal static class DisplayUriRequestOption
{
    private const string Key = "OpenUrzednik.DisplayUri";

#if NET
    private static readonly HttpRequestOptionsKey<string> OptionKey = new(Key);
#endif

    /// <summary>Attaches <paramref name="displayUri"/> to <paramref name="request"/>.</summary>
    internal static void Set(HttpRequestMessage request, string displayUri)
    {
#if NET
        request.Options.Set(OptionKey, displayUri);
#else
        request.Properties[Key] = displayUri;
#endif
    }

    /// <summary>Gets the display URI attached to <paramref name="request"/>, if any.</summary>
    internal static bool TryGet(HttpRequestMessage request, [NotNullWhen(true)] out string? displayUri)
    {
#if NET
        return request.Options.TryGetValue(OptionKey, out displayUri);
#else
        // No pattern variable in the condition: its mutants wouldn't compile and Stryker would discard them (ADR-0020).
        if (request.Properties.TryGetValue(Key, out var value))
        {
            displayUri = value as string;
            return displayUri is not null;
        }

        displayUri = null;
        return false;
#endif
    }
}
