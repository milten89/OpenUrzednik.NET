using OpenUrzednik.Nbp.Options;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Options;

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

    [Theory]
    [InlineData("http://api.nbp.pl/api/")]
    [InlineData("api.nbp.pl")]
    [InlineData("https://api.nbp.pl/api/?format=json")]
    [InlineData(" ")]
    public void Validate_InvalidApiUrl_ThrowsArgumentExceptionForApiUrl(string apiUrl)
    {
        // Arrange
        var sut = new NbpOptions { ApiUrl = apiUrl };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(NbpOptions.ApiUrl));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveTimeout_ThrowsArgumentExceptionForTimeout(int seconds)
    {
        // Arrange
        var sut = new NbpOptions { Timeout = TimeSpan.FromSeconds(seconds) };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(NbpOptions.Timeout));
    }

    [Fact]
    public void Validate_InfiniteTimeout_DoesNotThrow()
    {
        // Arrange
        var sut = new NbpOptions { Timeout = Timeout.InfiniteTimeSpan };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }
}
