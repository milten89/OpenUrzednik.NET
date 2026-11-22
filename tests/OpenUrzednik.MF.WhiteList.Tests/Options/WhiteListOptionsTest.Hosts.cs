using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Options;

public sealed partial class WhiteListOptionsTest
{
    [Fact]
    public void DefaultApiUrl_IsTheProductionHostRoot()
    {
        // Act & Assert: read-only fields, not constants (ADR-0023), so a patch after a host move reaches apps without a rebuild
        WhiteListOptions.DefaultApiUrl.ShouldBe("https://wykazpodatnikowvat.mf.gov.pl/");
    }

    [Fact]
    public void TestApiUrl_IsTheTestHostRoot()
    {
        // Act & Assert
        WhiteListOptions.TestApiUrl.ShouldBe("https://wykazpodatnikowvat-test.mf.gov.pl/");
    }
}
