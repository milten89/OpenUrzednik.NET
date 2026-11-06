using System.Globalization;

namespace OpenUrzednik.TestCommon;

/// <summary>
/// Sets <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/> for the current
/// async flow and restores the previous values on dispose.
/// </summary>
/// <remarks>
/// Needs ICU culture data: with <c>InvariantGlobalization</c> enabled, creating a named culture (e.g. <c>pl-PL</c>) throws
/// <see cref="CultureNotFoundException"/>, so tests that use this class fail in such an environment.
/// </remarks>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
