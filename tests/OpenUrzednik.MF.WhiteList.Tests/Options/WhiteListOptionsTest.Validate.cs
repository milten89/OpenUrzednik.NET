using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Options;

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

    [Theory]
    [InlineData("http://wykazpodatnikowvat.mf.gov.pl/")]
    [InlineData("wykazpodatnikowvat.mf.gov.pl")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/?format=json")]
    [InlineData(" ")]
    public void Validate_InvalidApiUrl_ThrowsArgumentExceptionForApiUrl(string apiUrl)
    {
        // Arrange
        var sut = new WhiteListOptions { ApiUrl = apiUrl };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.ApiUrl));
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyApiUrl_ThrowsArgumentExceptionSayingItIsEmpty(string apiUrl)
    {
        // Arrange
        var sut = new WhiteListOptions { ApiUrl = apiUrl };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.ApiUrl));
        exception.Message.ShouldStartWith("MF White List API url must not be empty.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveTimeout_ThrowsArgumentExceptionForTimeout(int seconds)
    {
        // Arrange
        var sut = new WhiteListOptions { Timeout = TimeSpan.FromSeconds(seconds) };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.Timeout));
    }

    [Fact]
    public void Validate_MaximumTimeout_DoesNotThrow()
    {
        // Arrange
        // HttpClient.Timeout accepts at most int.MaxValue milliseconds, and so does the option.
        var sut = new WhiteListOptions { Timeout = TimeSpan.FromMilliseconds(int.MaxValue) };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }

    [Fact]
    public void Validate_TimeoutAboveMaximum_ThrowsArgumentExceptionForTimeout()
    {
        // Arrange
        var sut = new WhiteListOptions { Timeout = TimeSpan.FromMilliseconds(int.MaxValue) + TimeSpan.FromMilliseconds(1) };

        // Act
        var exception = Should.Throw<ArgumentException>(sut.Validate);

        // Assert
        exception.ParamName.ShouldBe(nameof(WhiteListOptions.Timeout));
    }

    [Fact]
    public void Validate_InfiniteTimeout_DoesNotThrow()
    {
        // Arrange
        var sut = new WhiteListOptions { Timeout = Timeout.InfiniteTimeSpan };

        // Act & Assert
        Should.NotThrow(sut.Validate);
    }
}
