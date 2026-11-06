using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikLoggerTest
{
    [Fact]
    public void Instance_AlwaysReturnSameInstance()
    {
        // Act
        var instance1 = NullOpenUrzednikLogger.Instance;
        var instance2 = NullOpenUrzednikLogger.Instance;

        // Assert
        ReferenceEquals(instance1, instance2).ShouldBeTrue();
    }
}
