namespace OpenUrzednik.IntegrationTests.MF.WhiteList;

/// <summary>
/// Response bodies of the MF White List API, stored in <c>MF/WhiteList/Fixtures</c> (also linked into the unit tests and the AOT smoke
/// app). Where each one comes from (captured on 2026-11-20 with <c>/verify-api</c>, as returned, nothing replaced):
/// <list type="bullet">
/// <item><description>The test host <c>wykazpodatnikowvat-test.mf.gov.pl</c>, with the ministry's official fictitious test data:
/// <c>search-nip.json</c> (test NIP 3245174504, "Nazwa Firmy 1"), <c>search-nip-not-found.json</c> (NIP 5260250274, which the test host
/// doesn't have), <c>search-bank-account-not-found.json</c> (a test account number), and the 400 answers
/// <c>error-400-wl-1xx.json</c> (WL-101 no date, WL-103 a future date, WL-107 an invalid REGON, WL-111 an invalid account number, WL-115 an
/// invalid NIP, WL-118 a date before the list's range).</description></item>
/// <item><description>The test host, captured on 2026-11-21, the batch searches with the same fictitious test data: <c>search-nips.json</c>
/// (3245174504, 5260250274, the invalid 1234567890 with its entry error WL-115, and 3245174504 again: a subject, one not on the test list,
/// a rejected one and a duplicate), <c>search-regons.json</c> (216973362 "Nazwa Firmy 13", 755016841 from the ministry's test data but not on the test list, the invalid
/// 123456789 with WL-107), <c>search-bank-accounts.json</c> (a test account number not on the list, an invalid one with WL-111), and
/// <c>error-400-wl-130.json</c> (31 NIPs, more than a batch takes).</description></item>
/// <item><description>Production <c>wykazpodatnikowvat.mf.gov.pl</c>, for the Ministry of Finance, a legal person whose entry is public:
/// <c>search-regon.json</c> (REGON 000002217) and <c>search-bank-account.json</c> (one of its accounts). The test host's subjects have no
/// account numbers, and its 14-digit test REGONs fail the API's own check (WL-107).</description></item>
/// <item><description>Not captured: <c>*.from-error-table.json</c> are built from the ministry's error-code table (WL-191 with 429, WL-195
/// with 400, WL-197 with 500), because the test host never answered them (ADR-0023).</description></item>
/// </list>
/// </summary>
internal static class WhiteListFixtures
{
    public static string Load(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MF", "WhiteList", "Fixtures", name));
}
