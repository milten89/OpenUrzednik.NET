using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public class OpenUrzednikResultTest
{
    [Fact]
    public void SuccessResult_ShouldBeSuccess()
    {
        // Act
        var result = OpenUrzednikResult.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void DefaultCtor_ShouldBeSuccess()
    {
        // Act
        var result = new OpenUrzednikResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void FailureResult_SingleError_ShouldBeFailure()
    {
        // Arrange
        var error = new TestError("Test error");

        // Act
        var result = OpenUrzednikResult.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
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
        var result = OpenUrzednikResult.Failure([error1, error2]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
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
        var result = new OpenUrzednikResult(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
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
        var result = new OpenUrzednikResult([error1, error2]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
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
        Should.Throw<ArgumentNullException>(() => new OpenUrzednikResult(error));
    }

    [Fact]
    public void FailureCtor_NoErrorThrouMultipleErrors_ShouldThrowInvalidOperationException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = [];

        // Act && Assert
        Should.Throw<InvalidOperationException>(() => new OpenUrzednikResult(errors));
    }

    [Fact]
    public void FailureCtor_NullErrorsThrouMultipleErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new OpenUrzednikResult(errors));
    }

    [Fact]
    public void FailureResult_NullError_ShouldThrowArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Failure(error));
    }

    [Fact]
    public void FailureResult_NullErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = [];

        // Act && Assert
        Should.Throw<InvalidOperationException>(() => OpenUrzednikResult.Failure(errors));
    }

    [Fact]
    public void FailureResult_NullErrorsThrouMultipleErrors_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<OpenUrzednikError> errors = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Failure(errors));
    }
}
