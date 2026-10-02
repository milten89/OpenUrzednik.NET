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
}
