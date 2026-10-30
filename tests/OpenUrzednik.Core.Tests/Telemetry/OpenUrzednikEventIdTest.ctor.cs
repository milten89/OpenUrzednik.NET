using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class OpenUrzednikEventIdTest
{
    [Fact]
    public void Ctor_IdAndName_SetsThem()
    {
        // Act
        var eventId = new OpenUrzednikEventId(1001, "RequestTimedOut");

        // Assert
        eventId.Id.ShouldBe(1001);
        eventId.Name.ShouldBe("RequestTimedOut");
    }

    [Fact]
    public void Ctor_NullName_ThrowsArgumentNullException()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new OpenUrzednikEventId(1, null!));

        // Assert
        exception.ParamName.ShouldBe("name");
    }
}
