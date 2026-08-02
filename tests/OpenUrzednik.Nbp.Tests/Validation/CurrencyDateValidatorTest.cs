using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Validation;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class CurrencyDateValidatorTest
{
    private const string RuleName = "currencyDate";
    private const string PropertyName = "date";
    private static readonly DateOnly MinDate = new(2002, 1, 2);

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act
        var exception = Record.Exception(() => new CurrencyDateValidator(null!, MinDate));
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("propertyName");
    }
    
    [Theory]
    [InlineData(2002, 1, 2)]
    [InlineData(2002, 1, 3)]
    [InlineData(2026, 8, 1)]
    public void Validate_DateOnOrAfterMinDate_ReturnsSuccess(int year, int month, int day)
    {
        // Arrange
        var validator = new CurrencyDateValidator(PropertyName, new DateOnly(year, month, day));

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(2002, 1, 1)]
    [InlineData(2001, 12, 31)]
    [InlineData(1990, 1, 1)]
    public void Validate_DateBeforeMinDate_ReturnsValidationError(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var validator = new CurrencyDateValidator(PropertyName, date);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Metadata["value"].ShouldBe(date);
        error.Message.ShouldContain(MinDate.ToString("d"));
    }

    [Fact]
    public void Name_Always_ReturnsCurrencyDate()
    {
        // Arrange
        var validator = new CurrencyDateValidator(PropertyName, MinDate);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}