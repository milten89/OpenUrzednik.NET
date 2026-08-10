using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Validation;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class GoldDateValidatorTest
{
    private const string RuleName = "goldDate";
    private const string PropertyName = "date";
    private static readonly DateOnly MinDate = new(2013, 1, 2);

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new GoldDateValidator(null!, MinDate))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(2013, 1, 2)]
    [InlineData(2013, 1, 3)]
    [InlineData(2026, 8, 1)]
    public void Validate_DateOnOrAfterMinDate_ReturnsSuccess(int year, int month, int day)
    {
        // Arrange
        var validator = new GoldDateValidator(PropertyName, new DateOnly(year, month, day));

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(2013, 1, 1)]
    [InlineData(2012, 12, 31)]
    [InlineData(1990, 1, 1)]
    public void Validate_DateBeforeMinDate_ReturnsValidationError(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var validator = new GoldDateValidator(PropertyName, date);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Metadata["value"].ShouldBe(date);
    }

    [Fact]
    public void Name_Always_ReturnsGoldDate()
    {
        // Arrange
        var validator = new GoldDateValidator(PropertyName, MinDate);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}
