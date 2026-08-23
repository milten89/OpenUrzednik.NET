using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class TableTypeValidatorTest
{
    private const string RuleName = "midTableType";
    private const string PropertyName = "table";

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new TableTypeValidator(null!, TableType.A))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(TableType.A)]
    [InlineData(TableType.B)]
    public void Validate_DefinedEnumValue_ReturnsSuccess(TableType table)
    {
        // Arrange
        var validator = new TableTypeValidator(PropertyName, table);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData((TableType)99)]
    [InlineData((TableType)(-1))]
    public void Validate_UndefinedEnumValue_ReturnsValidationError(TableType table)
    {
        // Arrange
        var validator = new TableTypeValidator(PropertyName, table);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Message.ShouldContain(nameof(TableType));
    }

    [Fact]
    public void Name_Always_ReturnsMidTableType()
    {
        // Arrange
        var validator = new TableTypeValidator(PropertyName, TableType.A);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}
