using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class MidTableTypeValidatorTest
{
    private const string RuleName = "midTableType";
    private const string PropertyName = "table";

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new MidTableTypeValidator(null!, MidTableType.A))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(MidTableType.A)]
    [InlineData(MidTableType.B)]
    public void Validate_DefinedEnumValue_ReturnsSuccess(MidTableType table)
    {
        // Arrange
        var validator = new MidTableTypeValidator(PropertyName, table);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData((MidTableType)99)]
    [InlineData((MidTableType)(-1))]
    public void Validate_UndefinedEnumValue_ReturnsValidationError(MidTableType table)
    {
        // Arrange
        var validator = new MidTableTypeValidator(PropertyName, table);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Message.ShouldContain(nameof(MidTableType));
    }

    [Fact]
    public void Name_Always_ReturnsMidTableType()
    {
        // Arrange
        var validator = new MidTableTypeValidator(PropertyName, MidTableType.A);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}
