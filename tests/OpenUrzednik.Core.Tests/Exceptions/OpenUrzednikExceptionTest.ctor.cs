using Shouldly;

namespace OpenUrzednik.Core.Tests.Exceptions;

public partial class OpenUrzednikExceptionTest
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_EmptyOrWhiteSpaceCode_ThrowsArgumentException(string code)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new CodeException(code)).ParamName.ShouldBe("code");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_EmptyOrWhiteSpaceCodeWithInnerException_ThrowsArgumentException(string code)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new CodeException(code, new InvalidOperationException("inner"))).ParamName.ShouldBe("code");
    }
}
