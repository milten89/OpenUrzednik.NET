using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public class DateOnlyExtensionsTest
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("pl-PL")]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("fa-IR")]
    public void ToIso8601String_AnyCulture_ReturnsGregorianIsoDate(string culture)
    {
        // Arrange
        using var _ = new CultureScope(culture);

        // Act
        var result = new DateOnly(2026, 1, 5).ToIso8601String();

        // Assert
        result.ShouldBe("2026-01-05");
    }
}
