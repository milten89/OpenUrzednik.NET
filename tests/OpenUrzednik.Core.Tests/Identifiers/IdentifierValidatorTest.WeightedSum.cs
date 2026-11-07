using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class IdentifierValidatorTest
{
    // Weights 1, 2, 3, …: on .NET, fewer than 8 take the loop only, 8 and 16 the vector part only, 9 and 13 both.
    [Theory]
    [InlineData("1234567", 0, 7, 140)]
    [InlineData("12345678", 0, 8, 204)]
    [InlineData("9912345678", 2, 8, 204)]
    [InlineData("99123456789", 2, 9, 285)]
    [InlineData("991234567890123", 2, 13, 359)]
    [InlineData("9876543210987654", 0, 16, 674)]
    public void WeightedSum_DigitsFromStart_ReturnsSumOfDigitsTimesWeights(string digits, int start, int weightCount, int expected)
    {
        // Arrange
        var weights = Enumerable.Range(1, weightCount).ToArray();

        // Act
        var sum = TestValidator.CallWeightedSum(digits, start, weights);

        // Assert
        sum.ShouldBe(expected);
    }

    [Theory]
    [InlineData("1234567", 0)]
    [InlineData("123456789", 2)]
    public void WeightedSum_MoreWeightsThanDigitsFromStart_ThrowsInsteadOfReadingPastTheDigits(string digits, int start)
    {
        // Arrange
        var weights = Enumerable.Range(1, 8).ToArray();

        // Act
        var exception = Record.Exception(() => TestValidator.CallWeightedSum(digits, start, weights));

        // Assert
#if NET
        // The vector part loads without bounds checks, so the range check guards it.
        exception.ShouldBeOfType<ArgumentOutOfRangeException>().ParamName.ShouldBe("weights");
#else
        exception.ShouldBeOfType<IndexOutOfRangeException>();
#endif
    }
}
