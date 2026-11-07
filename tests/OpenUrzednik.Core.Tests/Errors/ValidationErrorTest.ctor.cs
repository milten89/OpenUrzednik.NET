using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class ValidationErrorTest
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_EmptyOrWhiteSpaceRuleName_ThrowsArgumentException(string ruleName)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new ValidationError("message", ruleName, "name", 1)).ParamName.ShouldBe("ruleName");
    }
}
