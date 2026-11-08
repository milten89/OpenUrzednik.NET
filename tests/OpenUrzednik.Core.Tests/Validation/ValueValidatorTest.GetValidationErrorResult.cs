using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Validation;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Validation;

public partial class ValueValidatorTest
{
    [Fact]
    public void GetValidationErrorResult_OwnValue_ReturnsGenericErrorWithTheValuesType()
    {
        // Act
        var result = new FailingValidator("count", 300).FailWithOwnValue();

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<int>>();
        error.Value.ShouldBe(300);
        error.Name.ShouldBe("count");
        error.RuleName.ShouldBe("failing");
        error.Message.ShouldBe("own");
    }

    [Fact]
    public void GetValidationErrorResult_ObjectValue_ReturnsPlainValidationError()
    {
        // Act
        var result = new FailingValidator("count", 300).FailWithObject("other", "x");

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Name.ShouldBe("other");
        error.Metadata[ValidationError.ValueMetadataKey].ShouldBe("x");
        error.Message.ShouldBe("object");
    }

    private sealed class FailingValidator(string propertyName, int value) : ValueValidator<int>(propertyName, value)
    {
        public override string Name => "failing";

        public override OpenUrzednikResult Validate() => FailWithOwnValue();

        public OpenUrzednikResult FailWithOwnValue() => GetValidationErrorResult("own");

        public OpenUrzednikResult FailWithObject(string name, object value) => GetValidationErrorResult("object", name, value);
    }
}
