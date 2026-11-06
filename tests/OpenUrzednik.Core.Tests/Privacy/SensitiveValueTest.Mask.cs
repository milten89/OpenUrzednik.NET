using OpenUrzednik.Privacy;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Privacy;

public partial class SensitiveValueTest
{
    [Fact]
    public void Mask_Null_ReturnsNull()
    {
        // Act
        string? masked = SensitiveValue.Mask(null);

        // Assert
        masked.ShouldBeNull();
    }

    [Fact]
    public void Mask_Empty_ReturnsEmpty()
    {
        // Act
        string masked = SensitiveValue.Mask(string.Empty);

        // Assert
        masked.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("1", "*")]
    [InlineData("12", "**")]
    [InlineData("123", "***")]
    [InlineData("1234", "****")]
    public void Mask_AtMostFourCharacters_MasksEverything(string value, string expected)
    {
        // Act
        string masked = SensitiveValue.Mask(value);

        // Assert
        masked.ShouldBe(expected);
    }

    [Theory]
    [InlineData("12345", "*2345")]
    [InlineData("1234567890", "******7890")]
    [InlineData("12345678901234", "**********1234")]
    [InlineData("12345678901234567890123456", "**********************3456")]
    [InlineData("PL12345678901234567890123456", "************************3456")]
    public void Mask_LongerThanFourCharacters_ShowsLastFour(string value, string expected)
    {
        // Act
        string masked = SensitiveValue.Mask(value);

        // Assert
        masked.ShouldBe(expected);
        masked.Length.ShouldBe(value.Length);
    }

    [Theory]
    [InlineData("ab12-CD34 ef56", "**********ef56")]
    [InlineData("PL12 3456 7890", "**********7890")]
    public void Mask_NonDigits_MasksTheSameWay(string value, string expected)
    {
        // Act
        string masked = SensitiveValue.Mask(value);

        // Assert
        masked.ShouldBe(expected);
    }
}
