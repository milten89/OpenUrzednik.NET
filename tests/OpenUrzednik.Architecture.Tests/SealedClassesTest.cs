using ArchUnitNET.Fluent;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Transport.Http;

using Shouldly;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0011: public classes not designed for inheritance are sealed (sealing one later is a breaking change).
public class SealedClassesTest
{
    // The designed extension points that aren't abstract: Core's condition types, which the transport and providers derive
    // from, and the HTTP transport's subclasses, which providers derive their own details from (ADR-0021). The abstract ones
    // (OpenUrzednikError, OpenUrzednikException, ValueValidator<T>, RangeValidationError) need no entry.
    private static readonly Type[] DesignedForInheritance =
    [
        typeof(BadRequestError), typeof(NotFoundError), typeof(RateLimitExceededError), typeof(RequestTimeoutError),
        typeof(ServiceUnavailableError), typeof(UnauthorizedError), typeof(UnknownError), typeof(ValidationError),
        typeof(HttpBadRequestError), typeof(HttpNotFoundError), typeof(HttpRateLimitExceededError), typeof(HttpRequestTimeoutError),
        typeof(HttpServiceUnavailableError), typeof(HttpUnauthorizedError), typeof(HttpUnknownError),
    ];

    [Fact]
    public void PublicClass_NotDesignedForInheritance_IsSealed()
    {
        // Arrange
        // PublicSurface's types, not ArchUnitNET's ArePublic(): those include protected nested classes and leave out public nested
        // classes of internal types.
        IArchRule rule = Classes().That().Are(PublicSurface.Types()).And().AreNotAbstract().And().AreNot(DesignedForInheritance)
            .Should().BeSealed()
            .Because("ADR-0011: a public class not designed for inheritance is sealed");

        // Act & Assert
        rule.ShouldHold();
    }

    [Fact]
    public void DesignedForInheritance_Always_ListsOnlyUnsealedPublicClassesOfThePackages()
    {
        // Act
        var stale = DesignedForInheritance
            .Where(type => !type.IsPublic || !type.IsClass || type.IsSealed || type.IsAbstract || !SrcAssemblies.All.Contains(type.Assembly))
            .Select(type => type.FullName)
            .ToList();

        // Assert
        stale.ShouldBeEmpty("a listed type was sealed, made abstract or moved: update the list");
    }
}
