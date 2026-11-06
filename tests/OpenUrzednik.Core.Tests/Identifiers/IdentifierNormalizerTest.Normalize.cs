using OpenUrzednik.Identifiers;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class IdentifierNormalizerTest
{
    [Fact]
    public void Normalize_NothingToRemove_ReturnsSameInstance()
    {
        // Arrange
        const string value = "1234563218";

        // Act
        var normalized = IdentifierNormalizer.Normalize(value, allowCountryPrefix: true);

        // Assert
        normalized.ShouldBeSameAs(value);
    }

    [Theory]
    [InlineData("123-456-32-18", false, "1234563218")]
    [InlineData(" 12 34\t56\u00A032\u202F18 ", false, "1234563218")] // no-break and narrow no-break spaces, as banking apps copy them
    [InlineData("PL1234563218", true, "1234563218")]
    [InlineData("  pL-1234563218", true, "1234563218")]
    [InlineData("PL", true, "")]
    [InlineData("- -", false, "")]
    public void Normalize_SeparatorsOrPrefix_RemovesThem(string value, bool allowCountryPrefix, string expected)
    {
        // Act
        var normalized = IdentifierNormalizer.Normalize(value, allowCountryPrefix);

        // Assert
        normalized.ShouldBe(expected);
    }

    [Theory]
    [InlineData("PL1234563218", false)]
    [InlineData("12PL34", true)]
    [InlineData("P", true)]
    [InlineData("123\u2013456\u2011321", true)] // en dash and non-breaking hyphen aren't separators
    public void Normalize_PrefixNotAllowedOrNotLeading_KeepsIt(string value, bool allowCountryPrefix)
    {
        // Act
        var normalized = IdentifierNormalizer.Normalize(value, allowCountryPrefix);

        // Assert
        normalized.ShouldBe(value);
    }
}
