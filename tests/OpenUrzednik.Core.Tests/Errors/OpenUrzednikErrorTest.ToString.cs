using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class OpenUrzednikErrorTest
{
    [Fact]
    public void ToString_Always_ReturnsCodeAndMessage()
    {
        // Arrange
        var error = new NotFoundError("No rates for the date.");

        // Act
        var text = error.ToString();

        // Assert
        text.ShouldBe("notFound: No rates for the date.");
    }
}
