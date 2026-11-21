using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public class VatStatusTest
{
    [Fact]
    public void Values_StartAtOne_SoTheDefaultIsNoStatus()
    {
        // Assert: default(VatStatus) must never read as Active, the status that makes a payment safe (frozen at release)
        ((int)VatStatus.Active).ShouldBe(1);
        ((int)VatStatus.Exempt).ShouldBe(2);
        ((int)VatStatus.NotRegistered).ShouldBe(3);
        Enum.IsDefined(typeof(VatStatus), default(VatStatus)).ShouldBeFalse();
    }
}
