using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class OpenUrzednikErrorTest
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_EmptyOrWhiteSpaceCode_ThrowsArgumentException(string code)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new CodeError(code, "message")).ParamName.ShouldBe("code");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_EmptyOrWhiteSpaceMessage_ThrowsArgumentException(string message)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new CodeError("code", message)).ParamName.ShouldBe("message");
    }
}
