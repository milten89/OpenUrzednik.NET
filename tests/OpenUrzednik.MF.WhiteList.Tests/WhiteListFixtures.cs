namespace OpenUrzednik.MF.WhiteList.Tests;

/// <summary>
/// The response bodies of <c>tests/OpenUrzednik.IntegrationTests/MF/WhiteList/Fixtures</c>, linked into this project (see the
/// IntegrationTests' <c>WhiteListFixtures</c> for where each one comes from).
/// </summary>
internal static class WhiteListFixtures
{
    public static string Load(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
