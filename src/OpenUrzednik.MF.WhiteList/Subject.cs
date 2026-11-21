namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// A subject of the VAT White List (Wykaz podatników VAT) on the requested date: who it is, its VAT status, its registration history and
/// its bank accounts. Every member but <paramref name="Name"/> is <see langword="null"/> (or an empty list) when the register doesn't give it.
/// </summary>
/// <remarks>
/// <para>
/// When a VAT taxpayer pays for a transaction between businesses worth more than 15,000 PLN to an account that isn't on the list on the
/// day the transfer is ordered, the payer may become jointly liable for the supplier's VAT arrears and may not count the payment as a tax
/// cost (art. 117ba of the Tax Ordinance, art. 15d CIT, art. 22p PIT; exceptions apply). The library returns the data as the register gives
/// it and gives no legal advice.
/// </para>
/// <para>
/// A sole trader's entry is personal data (name, addresses, NIP, accounts): the library passes it to the caller and never logs it. The
/// caller is responsible for it under the GDPR. The register's PESEL fields aren't exposed: the list has carried no PESEL since 2021-01-01.
/// </para>
/// <para>
/// Dates are <see cref="DateTime"/> at 00:00 with <see cref="DateTimeKind.Unspecified"/>. Minor versions may add properties (ADR-0011).
/// </para>
/// </remarks>
/// <param name="Name">The company name, or the first and last name of a natural person.</param>
public sealed record Subject(string Name)
{
    /// <summary>Gets the NIP, 10 digits, as the register gives it.</summary>
    public string? Nip { get; init; }

    /// <summary>Gets the VAT status on the requested date (<c>statusVat</c>).</summary>
    public VatStatus? VatStatus { get; init; }

    /// <summary>Gets the REGON, 9 or 14 digits, as the register gives it.</summary>
    public string? Regon { get; init; }

    /// <summary>Gets the KRS number, 10 digits, as the register gives it.</summary>
    public string? Krs { get; init; }

    /// <summary>Gets the address of the registered office (for a company) or of residence (for a natural person), as one line.</summary>
    public string? ResidenceAddress { get; init; }

    /// <summary>Gets the address of the permanent place of business, as one line.</summary>
    public string? WorkingAddress { get; init; }

    /// <summary>Gets the people authorized to represent the subject; listed only for a subject refused registration or struck off.</summary>
    public IReadOnlyList<SubjectPerson> Representatives { get; init; } = [];

    /// <summary>Gets the proxies (<c>authorizedClerks</c>); listed only for a subject refused registration or struck off.</summary>
    public IReadOnlyList<SubjectPerson> AuthorizedClerks { get; init; } = [];

    /// <summary>Gets the partners; listed only for a subject refused registration or struck off.</summary>
    public IReadOnlyList<SubjectPerson> Partners { get; init; } = [];

    /// <summary>Gets the date of registration as a VAT taxpayer.</summary>
    public DateTime? RegistrationLegalDate { get; init; }

    /// <summary>Gets the date registration as a VAT taxpayer was refused.</summary>
    public DateTime? RegistrationDenialDate { get; init; }

    /// <summary>
    /// Gets the legal basis of the refusal; <see langword="null"/> when the register gives none (also for the empty text the test host
    /// sends).
    /// </summary>
    public string? RegistrationDenialBasis { get; init; }

    /// <summary>Gets the date the subject was restored to the register.</summary>
    public DateTime? RestorationDate { get; init; }

    /// <summary>
    /// Gets the legal basis of the restoration; <see langword="null"/> when the register gives none (also for the empty text the test
    /// host sends).
    /// </summary>
    public string? RestorationBasis { get; init; }

    /// <summary>Gets the date the subject was struck off the register.</summary>
    public DateTime? RemovalDate { get; init; }

    /// <summary>
    /// Gets the legal basis of the striking off; <see langword="null"/> when the register gives none (also for the empty text the test
    /// host sends).
    /// </summary>
    public string? RemovalBasis { get; init; }

    /// <summary>Gets the date of the SME exemption (<c>exemptionSmeDate</c>, <c>Data zwolnienia SME</c>).</summary>
    public DateTime? ExemptionSmeDate { get; init; }

    /// <summary>
    /// Gets the subject's settlement accounts on the list (26-digit NRB numbers, as the register gives them); empty when it has none.
    /// </summary>
    public IReadOnlyList<string> AccountNumbers { get; init; } = [];

    /// <summary>
    /// Gets whether the subject has virtual accounts (<c>hasVirtualAccounts</c>): payments to an account it lists may go through accounts
    /// not shown here. <see langword="null"/> when the register doesn't say.
    /// </summary>
    public bool? HasVirtualAccounts { get; init; }

    /// <summary>Compares all members, and the lists item by item (a record would compare the list references).</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(Subject? other)
        => other is not null &&
           Name == other.Name &&
           Nip == other.Nip &&
           VatStatus == other.VatStatus &&
           Regon == other.Regon &&
           Krs == other.Krs &&
           ResidenceAddress == other.ResidenceAddress &&
           WorkingAddress == other.WorkingAddress &&
           Representatives.SequenceEqual(other.Representatives) &&
           AuthorizedClerks.SequenceEqual(other.AuthorizedClerks) &&
           Partners.SequenceEqual(other.Partners) &&
           RegistrationLegalDate == other.RegistrationLegalDate &&
           RegistrationDenialDate == other.RegistrationDenialDate &&
           RegistrationDenialBasis == other.RegistrationDenialBasis &&
           RestorationDate == other.RestorationDate &&
           RestorationBasis == other.RestorationBasis &&
           RemovalDate == other.RemovalDate &&
           RemovalBasis == other.RemovalBasis &&
           ExemptionSmeDate == other.ExemptionSmeDate &&
           AccountNumbers.SequenceEqual(other.AccountNumbers) &&
           HasVirtualAccounts == other.HasVirtualAccounts;

    /// <inheritdoc/>
    public override int GetHashCode()
        => (Name, Nip, Regon, AccountNumbers.Count).GetHashCode();
}
