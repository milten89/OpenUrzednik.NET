namespace OpenUrzednik.Nbp.UrlBuilder;

/// <summary>
/// NBP rate table, as used in request paths.
/// </summary>
public enum NbpTable
{
    /// <summary>Table A: mid rates of common currencies.</summary>
    A,
    /// <summary>Table B: mid rates of less common currencies.</summary>
    B,
    /// <summary>Table C: bid and ask rates.</summary>
    C,
}
