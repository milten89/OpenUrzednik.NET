using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class OpenUrzednikEventIdTest
{
    [Fact]
    public void ToString_Named_ReturnsName()
    {
        // Act & Assert
        new OpenUrzednikEventId(1001, "RequestTimedOut").ToString().ShouldBe("RequestTimedOut");
    }

    [Fact]
    public void ToString_Default_ReturnsId()
    {
        // Act & Assert
        default(OpenUrzednikEventId).ToString().ShouldBe("0");
    }
}
