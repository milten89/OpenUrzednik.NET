using ArchUnitNET.Fluent;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0007: no public .Extensions or .Options namespaces in Core or the providers. Package names (OpenUrzednik.Extensions.Logging)
// and the namespace DI extension methods conventionally use (Microsoft.Extensions.DependencyInjection) are exempt.
public class NamespacesTest
{
    [Fact]
    public void PublicType_Always_HasNoExtensionsOrOptionsNamespaceSegment()
    {
        // Arrange
        IArchRule rule = Types().That().ArePublic()
            .And().DoNotResideInNamespace("OpenUrzednik.Extensions.Logging")
            .And().DoNotResideInNamespace("Microsoft.Extensions.DependencyInjection")
            .Should().NotResideInNamespaceMatching(@"\.(Extensions|Options)(\.|$)")
            .Because("ADR-0007: no public .Extensions or .Options namespaces");

        // Act & Assert
        rule.ShouldHold();
    }
}
