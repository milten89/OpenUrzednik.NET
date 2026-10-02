using OpenUrzednik.Nbp.Options;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

public sealed partial class NbpOptionsValidationTest
{
    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        // Act
        var result = new NbpOptionsValidation().Validate(null, new NbpOptions { ApiUrl = "https://proxy.example.com/nbp/" });

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_HttpApiUrl_FailsWithTheClientMessage()
    {
        // Act
        var result = new NbpOptionsValidation().Validate(null, new NbpOptions { ApiUrl = "http://api.nbp.pl/api/" });

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("Invalid NBP API url scheme: http://api.nbp.pl/api/. NBP API no longer supports HTTP.");
    }

    [Fact]
    public void Validate_ZeroTimeout_Fails()
    {
        // Act
        var result = new NbpOptionsValidation().Validate(null, new NbpOptions { Timeout = TimeSpan.Zero });

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
