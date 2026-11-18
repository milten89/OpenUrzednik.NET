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
        using var json = JsonDocument.Parse(RepositoryFiles.Read("OpenUrzednik.Packages.slnf"));
        var packages = json.RootElement.GetProperty("solution").GetProperty("projects").EnumerateArray()
            .Select(project => Path.GetFileNameWithoutExtension(project.GetString()!.Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Act
        var tested = SrcAssemblies.All.Select(assembly => assembly.GetName().Name!).OrderBy(name => name, StringComparer.Ordinal).ToList();

        // Assert
        tested.ShouldBe(packages, "add a type of each new package to SrcAssemblies.All (and a ProjectReference)");
    }

    // The ArchUnitNET rules select PublicSurface's types; one ArchUnitNET can't map would be skipped without a word.
    [Fact]
    public void ArchUnitModel_Always_KnowsEveryPublicType()
    {
        // Act
        var unknown = ArchUnitModel.Unknown(PublicSurface.Types()).Select(type => type.FullName).ToList();

        // Assert
        unknown.ShouldBeEmpty();
    }
}
