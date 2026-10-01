using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public class IntExtensionsTest
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("fa-IR")]
    [InlineData("sv-SE")]
    public void ToInvariantString_NegativeValueInAnyCulture_UsesAsciiMinusSign(string culture)
    {
        // Arrange
        using var _ = new CultureScope(culture);

        // Act
        var result = (-1234).ToInvariantString();

        // Assert
        result.ShouldBe("-1234");
    }
}
