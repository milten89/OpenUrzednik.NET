using System.Reflection;

using OpenUrzednik.Nbp.Currency;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests;

// ADR-0007: CancellationToken cancellationToken = default is the last parameter of every async method,
// in both interfaces and implementations.
public class ClientConventionsTest
{
    public static TheoryData<string> PublicAsyncMethods()
    {
        var data = new TheoryData<string>();
        foreach (var method in GetPublicAsyncMethods())
            data.Add($"{method.DeclaringType!.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
        return data;
    }

    [Theory]
    [MemberData(nameof(PublicAsyncMethods))]
    public void PublicAsyncMethod_LastParameterIsOptionalCancellationToken(string signature)
    {
        // Arrange
        var method = GetPublicAsyncMethods().Single(m =>
            $"{m.DeclaringType!.Name}.{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})" == signature);

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
            .Where(t => t.Namespace?.StartsWith("OpenUrzednik.Nbp", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType));
}
