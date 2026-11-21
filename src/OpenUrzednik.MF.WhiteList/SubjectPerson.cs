namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// A person or company named on a subject's entry: a representative, a proxy (<c>authorizedClerks</c>) or a partner. The register lists
/// them only for a subject refused registration or struck off (art. 96b ust. 3–4 of the VAT act). Each part is
/// <see langword="null"/> when the register doesn't give it; the register's PESEL field isn't exposed (ADR-0023).
/// </summary>
/// <remarks>
/// This is personal data of the people named: the library passes it to the caller and never logs it. The caller is responsible for it
/// under the GDPR.
/// </remarks>
/// <param name="CompanyName">The company name, for a company.</param>
/// <param name="FirstName">The first name, for a person.</param>
/// <param name="LastName">The last name, for a person.</param>
/// <param name="Nip">The NIP, 10 digits, as the register gives it.</param>
public sealed record SubjectPerson(string? CompanyName, string? FirstName, string? LastName, string? Nip);
