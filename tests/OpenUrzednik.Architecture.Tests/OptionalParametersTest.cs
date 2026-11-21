using System.Reflection;

using Shouldly;

namespace OpenUrzednik.Architecture.Tests;

// The public API analyzer's RS0026 and RS0027 (off in src/.editorconfig) count `CancellationToken cancellationToken = default`
// (ADR-0026) as an optional parameter, so they would flag every overload set of an async method. These tests keep what the two
// rules guard against, the token aside:
// - two overloads with the same required parameters and any optional ones (the token included) are ambiguous for a call that
//   passes only the required arguments (CS0121), so no two such overloads exist;
// - of a member's public overloads, only the one with the most parameters has optional parameters besides the token, so a new
//   option goes onto that overload or into a new overload, never onto a second one.
// Adding an optional parameter to an existing member is a binary break that package validation reports (ADR-0011).
public class OptionalParametersTest
{
    // Overload sets that shipped in 1.0.0 with `TableType table = TableType.A` on more than one overload, with their number of
    // overloads: a new overload changes the count and gets checked. Changing them is a major version (ADR-0011). Their calls
    // can't be ambiguous: the overloads differ in their required parameters (the first test checks that for them too).
    private static readonly Dictionary<string, int> Shipped = new()
    {
        ["OpenUrzednik.Nbp.INbpCurrencyExchangeRateClient.GetAsync"] = 2,
        ["OpenUrzednik.Nbp.NbpCurrencyExchangeRateClient.GetAsync"] = 2,
        ["OpenUrzednik.Nbp.NbpDateOnlyExtensions.GetAsync"] = 6, // the extensions of all three clients share the name
    };

    [Fact]
    public void PublicOverloads_SameRequiredParametersWithOptionalOnes_DontExist()
    {
        // Act
        var violations = OverloadSets()
            .SelectMany(set => set
                .Where(member => member.GetParameters().Any(parameter => parameter.IsOptional))
                .GroupBy(member => string.Join(", ", member.GetParameters().Where(parameter => !parameter.IsOptional)
                    .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)))
                .Where(group => group.Count() > 1)
                .Select(group => $"{set.Key}({group.Key})"))
            .ToList();

        // Assert
        violations.ShouldBeEmpty("a call with only the required arguments would match more than one overload");
    }

    [Fact]
    public void PublicOverloads_OptionalParametersBesidesTheToken_OnlyOnTheOverloadWithTheMostParameters()
    {
        // Arrange
        var sets = OverloadSets();

        // Act
        var violations = sets
            .Where(set => set.Count() > 1)
            .Where(set =>
            {
                var withOptional = set.Where(HasOptionalBesidesToken).ToList();
                var most = set.Max(member => member.GetParameters().Length);
                return withOptional.Count > 1 || withOptional.Any(member => member.GetParameters().Length < most);
            })
            .Where(set => !(Shipped.TryGetValue(set.Key, out var count) && count == set.Count()))
            .Select(set => $"{set.Key} ({set.Count()} overloads)")
            .ToList();

        // Assert
        sets.ShouldNotBeEmpty("no public member found: the member discovery broke");
        violations.ShouldBeEmpty();
    }

    // Each public method name or the constructors of a type, without obsolete members (the analyzer skips them too).
    private static List<IGrouping<string, MethodBase>> OverloadSets()
        => PublicSurface.Types()
            .SelectMany(type => PublicSurface.Methods(type).Cast<MethodBase>().Concat(PublicSurface.Constructors(type))
                .Where(member => member.GetCustomAttribute<ObsoleteAttribute>() is null)
                .GroupBy(member => $"{type.FullName}.{member.Name}"))
            .ToList();

    private static bool HasOptionalBesidesToken(MethodBase member)
        => member.GetParameters().Any(parameter => parameter.IsOptional && parameter.ParameterType != typeof(CancellationToken));
}
