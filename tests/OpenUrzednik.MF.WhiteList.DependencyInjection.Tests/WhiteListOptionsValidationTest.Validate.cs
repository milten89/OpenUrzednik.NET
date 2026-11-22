using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

public sealed partial class WhiteListOptionsValidationTest
{
    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        // Act
        var result = new WhiteListOptionsValidation().Validate(null, new WhiteListOptions { ApiUrl = "https://proxy.example.com/" });

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_HttpApiUrl_FailsWithTheClientMessage()
    {
        // Act
        var result = new WhiteListOptionsValidation().Validate(null, new WhiteListOptions { ApiUrl = "http://wykazpodatnikowvat.mf.gov.pl/" });

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("Invalid MF White List API url scheme: http://wykazpodatnikowvat.mf.gov.pl/. MF White List API supports only HTTPS.");
    }

    [Fact]
    public void Validate_ZeroTimeout_Fails()
    {
        // Act
        var result = new WhiteListOptionsValidation().Validate(null, new WhiteListOptions { Timeout = TimeSpan.Zero });

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
