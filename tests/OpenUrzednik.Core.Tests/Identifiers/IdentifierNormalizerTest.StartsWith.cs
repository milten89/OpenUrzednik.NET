using OpenUrzednik.Identifiers;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class IdentifierNormalizerTest
{
    [Theory]
    [InlineData("PL1234563218", true)]
    [InlineData("pl1234563218", true)]
    [InlineData(" p-L 1234563218", true)]
    [InlineData("PL", true)]
    [InlineData("DE123456789", false)]
    [InlineData("P", false)]
    [InlineData("12PL34", false)]
    [InlineData("", false)]
    public void StartsWith_PrefixIgnoringSeparatorsAndCase_ReturnsWhetherItMatches(string value, bool expected)
    {
        // Act
        var startsWith = IdentifierNormalizer.StartsWith(value, "PL");

        // Assert
        startsWith.ShouldBe(expected);
    }
}
