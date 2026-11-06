using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void TryGetValue_Success_ReturnsTrueAndValue()
    {
        // Act
        var found = OpenUrzednikResult.Success("value").TryGetValue(out var value);

        // Assert
        found.ShouldBeTrue();
        value.ShouldBe("value");
    }

    [Fact]
    public void TryGetValue_Failure_ReturnsFalseAndDefault()
    {
        // Act
        var found = OpenUrzednikResult.Failure<string>(new TestError("Test error")).TryGetValue(out var value);

        // Assert
        found.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public void TryGetValue_Default_ReturnsFalse()
    {
        // Act
        var found = default(OpenUrzednikResult<int>).TryGetValue(out var value);

        // Assert
        found.ShouldBeFalse();
        value.ShouldBe(0);
    }
}
