using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class OpenUrzednikLoggerTest
{
    [Fact]
    public void Ctor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new OpenUrzednikLogger(null!));

        // Assert
        exception.ParamName.ShouldBe("logger");
    }
}
