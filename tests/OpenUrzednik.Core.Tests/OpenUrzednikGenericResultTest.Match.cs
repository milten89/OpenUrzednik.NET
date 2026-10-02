using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void Match_Success_CallsOnSuccessWithValue()
    {
        // Act
        var value = OpenUrzednikResult.Success(42).Match(v => v + 1, errors => -errors.Count);

        // Assert
        value.ShouldBe(43);
    }

    [Fact]
    public void Match_Failure_CallsOnFailureWithErrors()
    {
        // Act
        var message = OpenUrzednikResult.Failure<int>(new TestError("Test error")).Match(_ => "ok", errors => errors[0].Message);

        // Assert
        message.ShouldBe("Test error");
    }

    [Fact]
    public void Match_Default_CallsOnFailureWithUninitializedError()
    {
        // Act
        var error = default(OpenUrzednikResult<int>).Match(_ => null, errors => errors[0]);

        // Assert
        error.ShouldBeOfType<UnknownError>();
    }

    [Fact]
    public void Match_NullOnFailure_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Success(1).Match(v => v, null!));
    }
}
