using System.Runtime.CompilerServices;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void SuccessResult_ShouldBeSuccess()
    {
        // Arrange
        var value = 42;

        // Act
        var result = OpenUrzednikResult.Success(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(value);
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void SuccessCtor_ShouldBeSuccess()
    {
        // Arrange
        var value = 42;

        // Act
        var result = new OpenUrzednikResult<int>(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(value);
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void DefaultCtor_ShouldThrowInvalidOperationException()
    {
        // Act && Assert
        Should.Throw<InvalidOperationException>(() => new OpenUrzednikResult<int>());
    }

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

    [Fact]
    public void ImplicitConversion_FailedNonGenericResult_ShouldBeFailureWithSameErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var source = OpenUrzednikResult.Failure(error);

        // Act
        OpenUrzednikResult<int> result = source;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void SuccessResult_NullValue_ShouldBeSuccess()
    {
        // Act
        var result = OpenUrzednikResult.Success<string?>(null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void FailureResult_SingleError_ShouldBeFailure()
    {
        // Arrange
        var error = new TestError("Test error");

        // Act
        var result = OpenUrzednikResult.Failure<int>(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBe(error);
    }

    [Fact]
    public void FailureResult_MultipleErrors_ShouldBeFailure()
    {
        // Arrange
        var error1 = new TestError("Test error 1");
        var error2 = new TestError("Test error 2");

        // Act
        var result = OpenUrzednikResult.Failure<int>([error1, error2]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
        result.Errors.Count.ShouldBe(2);
        result.Errors[0].ShouldBe(error1);
        result.Errors[1].ShouldBe(error2);
    }

    [Fact]
    public void FailureCtor_SingleError_ShouldBeFailure()
    {
        // Arrange
        var error = new TestError("Test error");

        // Act
        var result = new OpenUrzednikResult<int>(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBe(error);
    }

    [Fact]
    public void FailureCtor_MultipleErrors_ShouldBeFailure()
    {
        // Arrange
        var error1 = new TestError("Test error 1");
        var error2 = new TestError("Test error 2");

        // Act
        var result = new OpenUrzednikResult<int>([error1, error2]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
        result.Errors.Count.ShouldBe(2);
        result.Errors[0].ShouldBe(error1);
        result.Errors[1].ShouldBe(error2);
    }

    [Fact]
    public void FailureCtor_NullError_ShouldThrowArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new OpenUrzednikResult<int>(error));
    }

    [Fact]
    public void FailureCtor_NoErrorThrouMultipleErrors_ShouldThrowInvalidOperationException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = [];

        // Act && Assert
        Should.Throw<InvalidOperationException>(() => new OpenUrzednikResult<int>(errors));
    }

    [Fact]
    public void FailureCtor_NullErrorsThrouMultipleErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new OpenUrzednikResult<int>(errors));
    }

    [Fact]
    public void FailureResult_NullError_ShouldThrowArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Failure<int>(error));
    }

    [Fact]
    public void FailureResult_NullErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = [];

        // Act && Assert
        Should.Throw<InvalidOperationException>(() => OpenUrzednikResult.Failure<int>(errors));
    }

    [Fact]
    public void FailureResult_NullErrorsThrouMultipleErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Failure<int>(errors));
    }

    [Fact]
    public void ImplicitOperator_FailedNonGenericResult_KeepsAllErrors()
    {
        // Arrange
        OpenUrzednikError[] errors = [new TestError("first"), new TestError("second")];

        // Act
        OpenUrzednikResult<int> result = OpenUrzednikResult.Failure(errors);

        // Assert
        result.Errors.ShouldBe(errors);
    }
}
