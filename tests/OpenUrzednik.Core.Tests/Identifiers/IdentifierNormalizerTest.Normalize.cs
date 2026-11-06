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
        var normalized = IdentifierNormalizer.Normalize(value);

        // Assert
        normalized.ShouldBeSameAs(value);
    }

    [Theory]
    [InlineData("123-456-32-18", "1234563218")]
    [InlineData(" 12 34	56\u00A032\u202F18 ", "1234563218")] // no-break and narrow no-break spaces, as banking apps copy them
    [InlineData("PL 123-456-32-18", "PL1234563218")] // a prefix is the validator's rule: kept
    [InlineData("- -", "")]
    public void Normalize_Separators_RemovesThem(string value, string expected)
    {
        // Act
        var normalized = IdentifierNormalizer.Normalize(value);

        // Assert
        normalized.ShouldBe(expected);
    }

    [Fact]
    public void Normalize_OtherDashes_KeepsThem()
    {
        // Arrange
        const string value = "123\u2013456\u2011321"; // en dash and non-breaking hyphen aren't separators

        // Act
        var normalized = IdentifierNormalizer.Normalize(value);

        // Assert
        normalized.ShouldBe(value);
    }

    [Theory]
    [InlineData("PL1234563218", 2, "1234563218")]
    [InlineData(" PL 123-456-32-18", 2, "1234563218")]
    [InlineData("P-L1234563218", 2, "1234563218")]
    [InlineData("PL", 2, "")]
    public void Normalize_Skip_RemovesThatManyCharactersAfterTheSeparators(string value, int skip, string expected)
    {
        // Act
        var normalized = IdentifierNormalizer.Normalize(value, skip);

        // Assert
        normalized.ShouldBe(expected);
    }
}
