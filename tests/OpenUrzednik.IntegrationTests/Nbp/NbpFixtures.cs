namespace OpenUrzednik.IntegrationTests.Nbp;

/// <summary>
/// Response bodies captured from the real NBP API, stored in <c>Nbp/Fixtures</c>.
/// </summary>
internal static class NbpFixtures
{
    public static string Load(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Nbp", "Fixtures", name));
}
