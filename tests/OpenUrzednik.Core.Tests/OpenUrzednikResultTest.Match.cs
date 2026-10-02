using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultTest
{
    [Fact]
    public void Match_Success_CallsOnSuccess()
    {
        // Act
        var message = OpenUrzednikResult.Success().Match(() => "ok", errors => errors[0].Message);

        // Assert
        message.ShouldBe("ok");
    }

    [Fact]
    public void Match_Failure_CallsOnFailureWithErrors()
    {
        // Act
        var message = OpenUrzednikResult.Failure(new TestError("Test error")).Match(() => "ok", errors => errors[0].Message);

        // Assert
        message.ShouldBe("Test error");
    }

    [Fact]
    public void Match_NullOnSuccess_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Success().Match<int>(null!, errors => errors.Count));
    }
}
