namespace OpenUrzednik.MF.WhiteList.Search;

/// <summary>
/// A subject's status as a VAT taxpayer on the requested date (<c>statusVat</c>).
/// The values start at 1, so <see langword="default"/>(<see cref="VatStatus"/>) is no status, never <see cref="Active"/>.
/// </summary>
public enum VatStatus
{
    /// <summary>
    /// Registered as an active VAT taxpayer (<c>Czynny</c>).
    /// </summary>
    Active = 1,

    /// <summary>
    /// Registered as a VAT taxpayer exempt from VAT (<c>Zwolniony</c>).
    /// </summary>
    Exempt = 2,

    /// <summary>
    /// Not registered as a VAT taxpayer (<c>Niezarejestrowany</c>): refused registration or struck off the register.
    /// </summary>
    NotRegistered = 3,
}
