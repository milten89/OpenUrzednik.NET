using System.Reflection;

using OpenUrzednik.Nbp.Currency;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests;

// ADR-0007: CancellationToken cancellationToken = default is the last parameter of every async method,
// in both interfaces and implementations.
public class ClientConventionsTest
{
    private static readonly Type[] AsyncReturnTypes =
        [typeof(Task), typeof(Task<>), typeof(ValueTask), typeof(ValueTask<>), typeof(IAsyncEnumerable<>)];

    public static TheoryData<string> PublicAsyncMethods()
    {
        var data = new TheoryData<string>();
        foreach (var method in GetPublicAsyncMethods())
            data.Add(Describe(method));
        return data;
    }

    [Theory]
    [MemberData(nameof(PublicAsyncMethods))]
    public void PublicAsyncMethod_Always_LastParameterIsOptionalCancellationToken(string signature)
    {
        // Arrange
        var method = GetPublicAsyncMethods().Single(m => Describe(m) == signature);

        // Act
        var last = method.GetParameters().LastOrDefault();

        // Assert
        last.ShouldNotBeNull();
        last.ParameterType.ShouldBe(typeof(CancellationToken));
        last.Name.ShouldBe("cancellationToken");
        last.HasDefaultValue.ShouldBeTrue();
    }

    private static IEnumerable<MethodInfo> GetPublicAsyncMethods()
        => typeof(NbpCurrencyExchangeRateClient).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => AsyncReturnTypes.Contains(m.ReturnType.IsGenericType ? m.ReturnType.GetGenericTypeDefinition() : m.ReturnType));

    // Full type names keep signatures unique if two public types ever share a name across namespaces.
    private static string Describe(MethodInfo method)
        => $"{method.DeclaringType!.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.ToString()))})";
}
