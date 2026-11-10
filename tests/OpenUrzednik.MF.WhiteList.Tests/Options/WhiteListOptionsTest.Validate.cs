using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Options;

// The shared URL and timeout rules are tested in OpenUrzednik.Http.Tests (RestOptionsTest); these tests check that Validate applies them
// and the White List rule on top of them.
public sealed partial class WhiteListOptionsTest
{
    [Fact]
    public void Validate_Defaults_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => new WhiteListOptions().Validate());
    }

    [Theory]
    [InlineData(WhiteListOptions.DefaultApiUrl)]
    [InlineData(WhiteListOptions.TestApiUrl)]
    [InlineData("https://proxy.example.com/white-list")]
    public void Validate_ValidValues_DoesNotThrow(string apiUrl)
    {
        // Arrange
        var sut = new WhiteListOptions { ApiUrl = apiUrl, Timeout = TimeSpan.FromSeconds(5) };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }

    [Fact]
    public void Validate_InvalidApiUrl_ThrowsArgumentExceptionForApiUrl()
    {
        // Arrange
        var sut = new WhiteListOptions { ApiUrl = "http://wykazpodatnikowvat.mf.gov.pl/" };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.ApiUrl));
        exception.Message.ShouldStartWith("Invalid MF White List API url scheme: http://wykazpodatnikowvat.mf.gov.pl/. MF White List API supports only HTTPS.");
    }

    [Theory]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api/")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api2")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api2/")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/API/")]
    [InlineData("https://wykazpodatnikowvat-test.mf.gov.pl/Api2")]
    [InlineData("https://proxy.example.com/white-list/api/")]
    public void Validate_ApiUrlEndingWithAnApiPath_ThrowsArgumentExceptionAskingForTheHostRoot(string apiUrl)
    {
        // Arrange
        var sut = new WhiteListOptions { ApiUrl = apiUrl };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.ApiUrl));
        exception.Message.ShouldContain("Set the host root (e.g. https://wykazpodatnikowvat.mf.gov.pl/)");
    }

    [Theory]
    [InlineData("https://proxy.example.com/white-list-api/")]
    [InlineData("https://proxy.example.com/apiary")]
    [InlineData("https://proxy.example.com/api3/")]
    [InlineData("https://proxy.example.com/api/white-list/")]
    public void Validate_ApiUrlWithApiOnlyInsideThePath_DoesNotThrow(string apiUrl)
    {
        // Arrange
        var sut = new WhiteListOptions { ApiUrl = apiUrl };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }

    [Fact]
    public void Validate_InvalidTimeout_ThrowsArgumentExceptionForTimeout()
    {
        // Arrange
        var sut = new WhiteListOptions { Timeout = TimeSpan.Zero };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.Timeout));
        exception.Message.ShouldStartWith("Invalid MF White List API timeout: ");
    }
}
