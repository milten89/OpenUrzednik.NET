using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

// The range itself is tested on IsValidTimeout; these check ValidateTimeout's own exception.
public partial class RestOptionsTest
{
    [Fact]
    public void ValidateTimeout_ValidTimeout_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => RestOptions.ValidateTimeout(TimeSpan.FromSeconds(5), DisplayName, "timeout"));
    }

    [Fact]
    public void ValidateTimeout_InvalidTimeout_ThrowsArgumentException()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(-5);

        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ValidateTimeout(timeout, DisplayName, "timeout"));

        // Assert
        exception.ParamName.ShouldBe("timeout");
        exception.Message.ShouldStartWith($"Invalid Test API timeout: {timeout}. It must be positive and at most {int.MaxValue} ms");
    }
}
