using OpenUrzednik.MF.WhiteList.Search;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

// Shared helpers; tests are in one file per method.
public sealed partial class SubjectTest
{
    // Each call builds new lists, so equality can't come from the list references.
    private static Subject Create()
        => new("Nazwa Firmy 1")
        {
            Nip = "3245174504",
            VatStatus = VatStatus.Active,
            Regon = "79156739856513",
            Krs = "1234567890",
            ResidenceAddress = "TAKA a Taka 191/369, 15-360 Miejscowość 1",
            WorkingAddress = "Inna 1, 00-001 Warszawa",
            Representatives = [new SubjectPerson(null, "Jan", "Kowalski", "7368125416")],
            AuthorizedClerks = [new SubjectPerson(null, "Anna", "Nowak", null)],
            Partners = [new SubjectPerson("Spółka", null, null, "5260250274")],
            RegistrationLegalDate = new DateTime(2017, 11, 26),
            RegistrationDenialDate = new DateTime(2018, 1, 2),
            RegistrationDenialBasis = "Art. 96 ust. 4",
            RestorationDate = new DateTime(2019, 3, 4),
            RestorationBasis = "Art. 96 ust. 9a",
            RemovalDate = new DateTime(2017, 10, 18),
            RemovalBasis = "Art. 96 ust. 9",
            ExemptionSmeDate = new DateTime(2025, 1, 1),
            AccountNumbers = ["59101010100075912222000000", "10101010100038252231000000"],
            HasVirtualAccounts = false,
        };

    // Each row changes one member, so a member Equals forgets fails its row.
    public static TheoryData<string> Changes =>
    [
        "Name", "Nip", "VatStatus", "Regon", "Krs", "ResidenceAddress", "WorkingAddress", "Representatives", "AuthorizedClerks", "Partners",
        "RegistrationLegalDate", "RegistrationDenialDate", "RegistrationDenialBasis", "RestorationDate", "RestorationBasis", "RemovalDate",
        "RemovalBasis", "ExemptionSmeDate", "AccountNumbers", "HasVirtualAccounts",
    ];

    private static Subject Change(Subject subject, string member) => member switch
    {
        "Name" => subject with { Name = "Nazwa Firmy 2" },
        "Nip" => subject with { Nip = "7368125416" },
        "VatStatus" => subject with { VatStatus = VatStatus.Exempt },
        "Regon" => subject with { Regon = "593728162" },
        "Krs" => subject with { Krs = "0000012345" },
        "ResidenceAddress" => subject with { ResidenceAddress = "Inna 2" },
        "WorkingAddress" => subject with { WorkingAddress = null },
        "Representatives" => subject with { Representatives = [new SubjectPerson(null, "Jan", "Kowalska", "7368125416")] },
        "AuthorizedClerks" => subject with { AuthorizedClerks = [] },
        "Partners" => subject with { Partners = [new SubjectPerson("Spółka", null, null, "5260250274"), new SubjectPerson("Inna", null, null, null)] },
        "RegistrationLegalDate" => subject with { RegistrationLegalDate = new DateTime(2017, 11, 27) },
        "RegistrationDenialDate" => subject with { RegistrationDenialDate = null },
        "RegistrationDenialBasis" => subject with { RegistrationDenialBasis = "Art. 96 ust. 4a" },
        "RestorationDate" => subject with { RestorationDate = new DateTime(2019, 3, 5) },
        "RestorationBasis" => subject with { RestorationBasis = null },
        "RemovalDate" => subject with { RemovalDate = new DateTime(2017, 10, 19) },
        "RemovalBasis" => subject with { RemovalBasis = "Art. 96 ust. 9b" },
        "ExemptionSmeDate" => subject with { ExemptionSmeDate = new DateTime(2025, 1, 2) },
        "AccountNumbers" => subject with { AccountNumbers = ["59101010100075912222000000", "10101010100038252231000001"] },
        "HasVirtualAccounts" => subject with { HasVirtualAccounts = true },
        _ => throw new ArgumentOutOfRangeException(nameof(member), member, null),
    };
}
