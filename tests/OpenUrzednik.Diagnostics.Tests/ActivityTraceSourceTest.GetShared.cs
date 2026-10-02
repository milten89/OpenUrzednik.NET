using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivityTraceSourceTest
{
    [Fact]
    public void GetShared_SameName_ReturnsSameInstance()
    {
        // Arrange
        var name = $"OpenUrzednik.Tests.{Guid.NewGuid():N}";

        // Act
        var first = ActivityTraceSource.GetShared(name);
        var second = ActivityTraceSource.GetShared(name);

        // Assert
        second.ShouldBeSameAs(first);
        first.Source.Name.ShouldBe(name);
    }

    [Fact]
    public void GetShared_DifferentNames_ReturnsDifferentSources()
    {
        // Act
        var first = ActivityTraceSource.GetShared($"OpenUrzednik.Tests.{Guid.NewGuid():N}");
        var second = ActivityTraceSource.GetShared($"OpenUrzednik.Tests.{Guid.NewGuid():N}");

        // Assert
        second.Source.ShouldNotBeSameAs(first.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetShared_EmptyName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => ActivityTraceSource.GetShared(name));
    }

    [Fact]
    public void GetShared_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ActivityTraceSource.GetShared(null!));
    }
}
