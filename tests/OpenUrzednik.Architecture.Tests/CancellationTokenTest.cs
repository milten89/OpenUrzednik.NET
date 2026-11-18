using System.Reflection;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// CLAUDE.md, Client API: CancellationToken cancellationToken = default is the last parameter of every async method, in both
// interfaces and implementations. In src/, CA1068 checks the order at build time; these tests check the name, the default, and
// that every public async method has one. Exempt: Core's result combinators (EnsureSuccessAsync, MapAsync, BindAsync), which
// continue an operation the caller started with its own token, and a method whose signature comes from outside the packages
// (IAsyncDisposable.DisposeAsync, an override of a BCL method).
public class CancellationTokenTest
{
    private static readonly string[] CombinatorNames = ["EnsureSuccessAsync", "MapAsync", "BindAsync"];

    private static readonly Type[] CombinatorTypes =
        [typeof(Core.OpenUrzednikResult), typeof(Core.OpenUrzednikResult<>), typeof(Core.OpenUrzednikResultExtensions)];

    [Fact]
    public void PublicMethod_WithCancellationToken_TakesItLastNamedAndOptional()
    {
        // Act
        var violations = PublicSurface.Types()
            .SelectMany(PublicSurface.Methods)
            .Where(method => method.GetParameters().Any(IsToken))
            .Where(method => !TakesTokenLastNamedAndOptional(method.GetParameters()))
            .Select(PublicSurface.Describe)
            .ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PublicAsyncMethod_ExceptCombinatorsAndExternalContracts_TakesACancellationToken()
    {
        // Arrange
        var asyncMethods = PublicSurface.Types()
            .SelectMany(PublicSurface.Methods)
            .Where(PublicSurface.ReturnsAsync)
            .Where(method => !IsResultCombinator(method) && !HasExternalContract(method))
            .ToList();

        // Act
        var violations = asyncMethods
            .Where(method => !method.GetParameters().Any(IsToken))
            .Select(PublicSurface.Describe)
            .ToList();

        // Assert
        asyncMethods.ShouldNotBeEmpty("no async method found: the method discovery broke");
        violations.ShouldBeEmpty();
    }

    private static bool TakesTokenLastNamedAndOptional(ParameterInfo[] parameters)
    {
        var last = parameters[parameters.Length - 1];
        return parameters.Count(IsToken) == 1 && IsToken(last) && last.Name == "cancellationToken" && last.HasDefaultValue;
    }

    private static bool IsResultCombinator(MethodInfo method)
        => CombinatorNames.Contains(method.Name)
           && CombinatorTypes.Contains(method.DeclaringType!.IsGenericType ? method.DeclaringType.GetGenericTypeDefinition() : method.DeclaringType);

    // An override of a base method, or an implementation of an interface method, declared outside the packages.
    private static bool HasExternalContract(MethodInfo method)
    {
        var type = method.DeclaringType!;
        if (!IsSrc(method.GetBaseDefinition().DeclaringType!))
            return true;
        return !type.IsInterface
               && type.GetInterfaces().Where(i => !IsSrc(i)).Any(i => type.GetInterfaceMap(i).TargetMethods.Contains(method));
    }

    private static bool IsSrc(Type type) => SrcAssemblies.All.Contains(type.Assembly);

    private static bool IsToken(ParameterInfo parameter) => parameter.ParameterType == typeof(CancellationToken);
}
