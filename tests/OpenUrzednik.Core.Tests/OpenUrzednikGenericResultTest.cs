using System.Runtime.CompilerServices;

using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void Size_ReferenceTypeValue_IsTwoPointers()
    {
        // The result is designed to stay two machine words wide (value + errors), so it can be passed in registers.
        Unsafe.SizeOf<OpenUrzednikResult<object>>().ShouldBe(2 * IntPtr.Size);
    }

    [Fact]
    public void DefaultValue_ShouldBeFailureWithUnknownError()
    {
        // Act
        var result = default(OpenUrzednikResult<string>);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnknownError>();
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void UninitializedArrayElement_ShouldBeFailureWithUninitializedError()
    {
        // Arrange
        var results = new OpenUrzednikResult<int>[1];

        // Act
        var result = results[0];

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnknownError>();
        error.Code.ShouldBe(UnknownError.ErrorCode);
        error.Message.ShouldContain("not initialized");
    }
}
