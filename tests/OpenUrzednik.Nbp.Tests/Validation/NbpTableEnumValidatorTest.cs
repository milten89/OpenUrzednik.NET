using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class NbpTableEnumValidatorTest
{
    private const string RuleName = "nbpTable";
    private const string PropertyName = "table";

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new NbpTableEnumValidator(null!, NbpTable.A))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(NbpTable.A)]
    [InlineData(NbpTable.B)]
    [InlineData(NbpTable.C)]
    public void Validate_DefinedEnumValue_ReturnsSuccess(NbpTable table)
    {
        // Arrange
        var validator = new NbpTableEnumValidator(PropertyName, table);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData((NbpTable)99)]
    [InlineData((NbpTable)(-1))]
    public void Validate_UndefinedEnumValue_ReturnsValidationError(NbpTable table)
    {
        // Arrange
        var validator = new NbpTableEnumValidator(PropertyName, table);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Message.ShouldContain(nameof(NbpTable));
    }

    [Fact]
    public void Name_Always_ReturnsNbpTable()
    {
        // Arrange
        var validator = new NbpTableEnumValidator(PropertyName, NbpTable.A);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}
