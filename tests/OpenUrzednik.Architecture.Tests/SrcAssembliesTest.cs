using System.Text.Json;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// Every rule here covers only the assemblies in SrcAssemblies.All, so the list must match the published packages: the projects
// of OpenUrzednik.Packages.slnf, which the NuGet workflows pack.
public class SrcAssembliesTest
{
    [Fact]
    public void All_Always_MatchesThePackagesSolutionFilter()
    {
        // Arrange
        var filter = FindUp("OpenUrzednik.Packages.slnf");
        using var json = JsonDocument.Parse(File.ReadAllText(filter));
        var packages = json.RootElement.GetProperty("solution").GetProperty("projects").EnumerateArray()
            .Select(project => Path.GetFileNameWithoutExtension(project.GetString()!.Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Act
        var tested = SrcAssemblies.All.Select(assembly => assembly.GetName().Name!).OrderBy(name => name, StringComparer.Ordinal).ToList();

        // Assert
        tested.ShouldBe(packages, "add a type of each new package to SrcAssemblies.All (and a ProjectReference)");
    }

    private static string FindUp(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, fileName);
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException($"{fileName} not found above {AppContext.BaseDirectory}.");
    }
}
