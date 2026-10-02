using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivityTraceSourceTest
{
    [Fact]
    public void Ctor_NullSource_ThrowsArgumentNullException()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new ActivityTraceSource(null!));

        // Assert
        exception.ParamName.ShouldBe("source");
    }
}
