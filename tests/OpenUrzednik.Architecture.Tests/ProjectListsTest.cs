using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// The lists a new project must join (/new-provider, CLAUDE.md, Workflow): a src/ project missing from one isn't published,
// trimmed, mutated or checked, and nothing else fails. Projects are compared by name: the file name without .csproj.
public class ProjectListsTest
{
    private static readonly Lazy<XDocument> AotSmoke = new(() => XDocument.Parse(RepositoryFiles.Read("tests/OpenUrzednik.AotSmoke/OpenUrzednik.AotSmoke.csproj")));

    private static readonly Lazy<List<string>> SrcProjects = new(() => RepositoryFiles.Projects("src").Select(Name).OrderBy(n => n, StringComparer.Ordinal).ToList());

    [Fact]
    public void SrcProject_Always_IsInThePackagesSolutionFilter()
    {
        // Arrange
        using var filter = JsonDocument.Parse(RepositoryFiles.Read("OpenUrzednik.Packages.slnf"));
        var listed = filter.RootElement.GetProperty("solution").GetProperty("projects").EnumerateArray().Select(p => Name(p.GetString()!));

        // Act & Assert
        Missing(listed).ShouldBeEmpty("add it to OpenUrzednik.Packages.slnf, or it isn't packed and published");
    }

    [Fact]
    public void SrcProject_Always_IsReferencedByTheAotSmokeApp()
    {
        // Act & Assert
        Missing(Includes(AotSmoke.Value, "ProjectReference").Select(Name))
            .ShouldBeEmpty("add a ProjectReference to tests/OpenUrzednik.AotSmoke (ADR-0014)");
    }

    [Fact]
    public void SrcProject_Always_IsATrimmerRootOfTheAotSmokeApp()
    {
        // Act & Assert
        Missing(Includes(AotSmoke.Value, "TrimmerRootAssembly"))
            .ShouldBeEmpty("add a TrimmerRootAssembly to tests/OpenUrzednik.AotSmoke, so every type is trimmed and AOT-analyzed (ADR-0014)");
    }

    [Fact]
    public void SrcProject_Always_IsMutatedByAShard()
    {
        // Arrange: the shards tests/Mutation/run.py knows (shards.json), each through its Stryker config's project.
        using var shards = JsonDocument.Parse(RepositoryFiles.Read("tests/Mutation/shards.json"));
        var mutated = shards.RootElement.GetProperty("shards").EnumerateObject()
            .Select(shard => JsonDocument.Parse(RepositoryFiles.Read(shard.Value.GetProperty("config").GetString()!)))
            .Select(config => Name(config.RootElement.GetProperty("stryker-config").GetProperty("project").GetString()!))
            .ToList();

        // Act & Assert
        Missing(mutated).ShouldBeEmpty("add a Stryker shard for it (tests/Mutation, ADR-0020; /stryker)");
    }

    [Fact]
    public void SrcProject_Always_IsReferencedByTheArchitectureTests()
    {
        // Arrange
        var tests = XDocument.Parse(RepositoryFiles.Read("tests/OpenUrzednik.Architecture.Tests/OpenUrzednik.Architecture.Tests.csproj"));

        // Act & Assert
        Missing(Includes(tests, "ProjectReference").Select(Name)).ShouldBeEmpty("add a ProjectReference here and a type of it to SrcAssemblies.All");
    }

    [Fact]
    public void Project_InSrcTestsOrSamples_IsInTheSolution()
    {
        // Arrange
        var solution = XDocument.Parse(RepositoryFiles.Read("OpenUrzednik.slnx"));
        var listed = solution.Descendants("Project").Select(project => (string?)project.Attribute("Path")).OfType<string>()
            .Select(path => path.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var missing = new[] { "src", "tests", "samples" }
            .SelectMany(RepositoryFiles.Projects)
            .Select(RepositoryFiles.Relative)
            .Where(path => !listed.Contains(path))
            .ToList();

        // Assert
        missing.ShouldBeEmpty("add it to OpenUrzednik.slnx, or CI never builds or tests it");
    }

    [Fact]
    public void Shard_Always_RunsInTheMutationWorkflow()
    {
        // Arrange
        using var shards = JsonDocument.Parse(RepositoryFiles.Read("tests/Mutation/shards.json"));
        var defined = shards.RootElement.GetProperty("shards").EnumerateObject().Select(shard => shard.Name).ToList();
        // A plain, quoted or flow-sequence value: `a b`, "a b", [a, b].
        var inMatrix = MatrixLines(RepositoryFiles.Read(".github/workflows/mutation.yml"))
            .Select(line => Regex.Match(line, @"^[ \t]*(?:-[ \t]+)?shards:[ \t]*(?<names>[^#]*)"))
            .Where(match => match.Success)
            .SelectMany(match => match.Groups["names"].Value.Split([' ', '\t', ',', '"', '\'', '[', ']'], StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var missing = defined.Where(shard => !inMatrix.Contains(shard)).ToList();

        // Assert
        defined.ShouldNotBeEmpty("no shard found in tests/Mutation/shards.json");
        missing.ShouldBeEmpty("add it to a shards: line of the mutation.yml matrix, or it never runs");
    }

    // The lines of the `matrix:` mapping: every line after it that is indented deeper (blank and comment lines included), so a
    // `shards:` key elsewhere in the workflow, such as a workflow_dispatch input, doesn't count.
    private static IEnumerable<string> MatrixLines(string workflow)
    {
        var lines = workflow.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        var start = lines.FindIndex(line => Regex.IsMatch(line, @"^[ \t]*matrix:[ \t]*(#.*)?$"));
        if (start < 0)
            yield break;
        var indent = Indent(lines[start]);
        foreach (var line in lines.Skip(start + 1))
        {
            if (line.Trim().Length > 0 && !line.TrimStart().StartsWith("#", StringComparison.Ordinal) && Indent(line) <= indent)
                yield break;
            yield return line;
        }
    }

    private static int Indent(string line) => line.Length - line.TrimStart(' ', '\t').Length;

    private static List<string> Missing(IEnumerable<string> listed)
    {
        var names = listed.ToHashSet(StringComparer.Ordinal);
        return SrcProjects.Value.Where(project => !names.Contains(project)).ToList();
    }

    private static IEnumerable<string> Includes(XDocument project, string item)
        => project.Descendants(item).Select(element => (string?)element.Attribute("Include")).OfType<string>();

    private static string Name(FileInfo project) => Path.GetFileNameWithoutExtension(project.Name);

    private static string Name(string path) => Path.GetFileNameWithoutExtension(path.Replace('\\', '/'));
}
