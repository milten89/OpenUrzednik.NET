namespace OpenUrzednik.Architecture.Tests;

/// <summary>Files of the repository the tests read, found from the test's output directory.</summary>
internal static class RepositoryFiles
{
    private static readonly Lazy<DirectoryInfo> RootDirectory = new(FindRoot);

    /// <summary>The repository root: the directory that holds <c>OpenUrzednik.slnx</c>.</summary>
    public static DirectoryInfo Root => RootDirectory.Value;

    /// <summary>The full path of <paramref name="relativePath"/> (with <c>/</c> separators) under the root.</summary>
    public static string PathOf(string relativePath)
        => Root.FullName + Path.DirectorySeparatorChar + relativePath.Replace('/', Path.DirectorySeparatorChar);

    public static string Read(string relativePath) => File.ReadAllText(PathOf(relativePath));

    /// <summary>A path relative to the root, with <c>/</c> separators, for messages and comparisons.</summary>
    public static string Relative(FileSystemInfo file)
        => file.FullName.Substring(Root.FullName.Length + 1).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>The project files one level below <paramref name="folder"/> (<c>src/*/*.csproj</c>).</summary>
    public static IEnumerable<FileInfo> Projects(string folder)
        => new DirectoryInfo(PathOf(folder)).EnumerateDirectories().SelectMany(directory => directory.EnumerateFiles("*.csproj"));

    private static DirectoryInfo FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("OpenUrzednik.slnx").Any())
                return directory;
        }

        throw new DirectoryNotFoundException($"OpenUrzednik.slnx not found above {AppContext.BaseDirectory}.");
    }
}
