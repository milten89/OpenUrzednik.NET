using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Options;

// The URL and timeout rules themselves are tested in OpenUrzednik.Http.Tests (RestOptionsTest); these tests check that Validate applies them.
public sealed partial class NbpOptionsTest
{
    [Fact]
    public void Validate_Defaults_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new NbpOptions().Validate());
    }

    [Fact]
    public void Validate_ValidValues_DoesNotThrow()
    {
        // Arrange
        var sut = new NbpOptions { ApiUrl = "https://proxy.example.com/nbp", Timeout = TimeSpan.FromSeconds(5) };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }

    [Fact]
    public void Validate_InvalidApiUrl_ThrowsArgumentExceptionForApiUrl()
    {
        // Arrange
        var sut = new NbpOptions { ApiUrl = "http://api.nbp.pl/api/" };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(NbpOptions.ApiUrl));
        exception.Message.ShouldStartWith("Invalid NBP API url scheme: http://api.nbp.pl/api/. NBP API supports only HTTPS.");
    }

    [Fact]
    public void Validate_InvalidTimeout_ThrowsArgumentExceptionForTimeout()
    {
        // Arrange
        var sut = new NbpOptions { Timeout = TimeSpan.Zero };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(NbpOptions.Timeout));
        exception.Message.ShouldStartWith("Invalid NBP API timeout: ");
    }
}
