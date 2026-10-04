using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikTraceSourceTest
{
    [Fact]
    public void Instance_AlwaysReturnSameInstance()
    {
        // Act
        var instance1 = NullOpenUrzednikTraceSource.Instance;
        var instance2 = NullOpenUrzednikTraceSource.Instance;

        // Assert
        ReferenceEquals(instance1, instance2).ShouldBeTrue();
    }
}
