using Shouldly;

namespace OpenUrzednik.Core.Tests.Validation;

public partial class ValueValidatorTest
{
    [Fact]
    public void Ctor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new TestValidator(null!, 1)).ParamName.ShouldBe("propertyName");
    }
}
