namespace OpenUrzednik.Nbp.UrlBuilder;

/// <summary>
/// Builds the request paths of one NBP API resource (a table, a currency in a table, or gold prices).
/// Pass a custom <see cref="INbpUrlBuilderFactory"/> to a client to change them.
/// </summary>
public interface INbpUrlBuilder
{
    /// <summary>Path of the latest published value.</summary>
    /// <returns>Request path.</returns>
    string Latest();
    /// <summary>Path of the last <paramref name="topCount"/> published values.</summary>
    /// <param name="topCount">Number of values.</param>
    /// <returns>Request path.</returns>
    string ForTopCount(int topCount);
    /// <summary>Path of today's value.</summary>
    /// <returns>Request path.</returns>
    string Today();
    /// <summary>Path of the value published on <paramref name="date"/>.</summary>
    /// <param name="date">Publication date.</param>
    /// <returns>Request path.</returns>
    string ForDate(DateOnly date);
    /// <summary>Path of the values published from <paramref name="from"/> to <paramref name="to"/>, inclusive.</summary>
    /// <param name="from">First date.</param>
    /// <param name="to">Last date.</param>
    /// <returns>Request path.</returns>
    string ForDateRange(DateOnly from, DateOnly to);
}
