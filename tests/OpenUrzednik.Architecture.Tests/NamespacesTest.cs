using ArchUnitNET.Fluent;

using Shouldly;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0026: no public .Extensions, .Options or .Models namespaces in Core or the providers. Package names (OpenUrzednik.Extensions.Logging)
// and the namespace DI extension methods conventionally use (Microsoft.Extensions.DependencyInjection) are exempt.
public class NamespacesTest
{
    [Fact]
    public void PublicType_Always_HasNoMemberKindNamespaceSegment()
    {
        // Arrange
        // PublicSurface's types, not ArchUnitNET's ArePublic(), which counts a public nested type of an internal type as public.
        IArchRule rule = Types().That().Are(PublicSurface.Types())
            .And().DoNotResideInNamespace("OpenUrzednik.Extensions.Logging")
            .And().DoNotResideInNamespace("Microsoft.Extensions.DependencyInjection")
            .Should().NotResideInNamespaceMatching(@"\.(Extensions|Options|Models)(\.|$)")
            .Because("ADR-0026: no public .Extensions, .Options or .Models namespaces");

        // Act & Assert
        rule.ShouldHold();
    }

    // ADR-0026: every public type of a provider package lives in the provider's root namespace (the assembly name).
    // Providers are the packages that aren't Core, a telemetry or transport package or a DI package.
    private static readonly string[] NonProviderAssemblies =
    [
        "OpenUrzednik.Core",
        "OpenUrzednik.Diagnostics",
        "OpenUrzednik.Extensions.Logging",
        "OpenUrzednik.Transport.Http",
    ];

    // NBP keeps its 1.x area namespaces until 2.0 (#351, ADR-0026).
    private static readonly string[] ProvidersKeepingAreaNamespaces = ["OpenUrzednik.Nbp"];

    [Fact]
    public void PublicType_OfAProvider_ResidesInTheRootNamespace()
    {
        // Arrange
        var providers = SrcAssemblies.All
            .Select(assembly => assembly.GetName().Name!)
            .Where(name => !NonProviderAssemblies.Contains(name) && !name.EndsWith(".DependencyInjection", StringComparison.Ordinal))
            .Where(name => !ProvidersKeepingAreaNamespaces.Contains(name))
            .ToList();

        // Act
        var misplaced = PublicSurface.Types()
            .Where(type => providers.Contains(type.Assembly.GetName().Name!))
            .Where(type => type.Namespace != type.Assembly.GetName().Name)
            .Select(type => type.FullName!)
            .ToList();

        // Assert
        providers.ShouldNotBeEmpty("a provider package is expected");
        misplaced.ShouldBeEmpty("ADR-0026: a provider's public types reside in its root namespace");
    }
}
