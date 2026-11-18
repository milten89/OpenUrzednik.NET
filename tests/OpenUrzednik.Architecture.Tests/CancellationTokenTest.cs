using System.Reflection;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// ADR-0007: CancellationToken cancellationToken = default is the last parameter of every async method, in both interfaces and
// implementations. In src/, CA1068 checks the order at build time; these tests check the name, the default, and that every async
// method has one. The one exemption: Core's result combinators (EnsureSuccessAsync, MapAsync, BindAsync, on a result or on a task
// of one), which continue an operation the caller started with its own token; a delegate they take closes over that token.
public class CancellationTokenTest
{
    [Fact]
    public void PublicMethod_WithCancellationToken_TakesItLastNamedAndOptional()
    {
        // Act
        var violations = PublicSurface.Types()
            .SelectMany(PublicSurface.Methods)
            .Where(method => method.GetParameters().Any(IsToken))
            .Where(method => method.GetParameters() is var parameters
                             && parameters[parameters.Length - 1] is var last
                             && (parameters.Count(IsToken) != 1 || !IsToken(last)
                                 || last.Name != "cancellationToken" || !last.HasDefaultValue))
            .Select(PublicSurface.Describe)
            .ToList();

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void PublicAsyncMethod_ExceptResultCombinators_TakesACancellationToken()
    {
        // Arrange
        var asyncMethods = PublicSurface.Types()
            .Where(type => !type.IsSubclassOf(typeof(Delegate)))
            .SelectMany(PublicSurface.Methods)
            .Where(PublicSurface.ReturnsAsync)
            .Where(method => !IsResultCombinator(method))
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

    // A method of OpenUrzednikResult / OpenUrzednikResult<T>, or an extension method whose receiver is a task (of a result).
    private static bool IsResultCombinator(MethodInfo method)
        => method.DeclaringType!.Assembly == typeof(Core.OpenUrzednikResult).Assembly
           && (method.DeclaringType.Name.StartsWith(nameof(Core.OpenUrzednikResult), StringComparison.Ordinal)
               || (method.IsStatic && method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
                   && method.GetParameters().FirstOrDefault() is { } receiver && PublicSurface.IsAsyncType(receiver.ParameterType)));

    private static bool IsToken(ParameterInfo parameter) => parameter.ParameterType == typeof(CancellationToken);
}
