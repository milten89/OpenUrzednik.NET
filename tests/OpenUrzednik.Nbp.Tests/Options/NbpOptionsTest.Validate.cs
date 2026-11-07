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
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyApiUrl_ThrowsArgumentExceptionSayingItIsEmpty(string apiUrl)
    {
        // Arrange
        var sut = new NbpOptions { ApiUrl = apiUrl };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(NbpOptions.ApiUrl));
        exception.Message.ShouldStartWith("NBP API url must not be empty.");
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
    public void Validate_MaximumTimeout_DoesNotThrow()
    {
        // Arrange
        // HttpClient.Timeout accepts at most int.MaxValue milliseconds, and so does the option.
        var sut = new NbpOptions { Timeout = TimeSpan.FromMilliseconds(int.MaxValue) };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }

    [Fact]
    public void Validate_TimeoutAboveMaximum_ThrowsArgumentExceptionForTimeout()
    {
        // Arrange
        var sut = new NbpOptions { Timeout = TimeSpan.FromMilliseconds(int.MaxValue) + TimeSpan.FromMilliseconds(1) };

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
