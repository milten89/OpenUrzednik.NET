using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestOptionsTest
{
    [Theory]
    [InlineData("https://api.example.com", "https://api.example.com/")]
    [InlineData("https://api.example.com/", "https://api.example.com/")]
    [InlineData("https://api.example.com/v1", "https://api.example.com/v1/")]
    [InlineData("https://api.example.com/v1/", "https://api.example.com/v1/")]
    [InlineData("https://api.example.com:8443/v1", "https://api.example.com:8443/v1/")]
    public void ParseBaseUrl_ValidUrl_AddsTrailingSlashOnlyWhenMissing(string url, string expected)
    {
        // Act
        var uri = RestOptions.ParseBaseUrl(url, DisplayName, ParamName);

        // Assert
        uri.IsAbsoluteUri.ShouldBeTrue();
        uri.ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ParseBaseUrl_EmptyUrl_ThrowsArgumentExceptionSayingItIsEmpty(string url)
    {
        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ParseBaseUrl(url, DisplayName, ParamName));

        // Assert
        exception.ParamName.ShouldBe(ParamName);
        exception.Message.ShouldStartWith("Test API url must not be empty.");
    }

    [Theory]
    [InlineData("api.example.com")]
    [InlineData("www.example.com/api")]
    [InlineData("/api/v1/")]
    [InlineData("https://")]
    [InlineData("https:/example.com")]
    [InlineData("htt ps://example.com")]
    [InlineData("https://example.com:abc/")]
    [InlineData("https://exa mple.com/")]
    public void ParseBaseUrl_NotAnAbsoluteUrl_ThrowsArgumentException(string url)
    {
        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ParseBaseUrl(url, DisplayName, ParamName));

        // Assert
        exception.ParamName.ShouldBe(ParamName);
        exception.Message.ShouldStartWith("Invalid Test API url");
    }

    [Fact]
    public void ParseBaseUrl_HttpUrl_ThrowsArgumentExceptionSayingOnlyHttpsIsSupported()
    {
        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ParseBaseUrl("http://api.example.com/", DisplayName, ParamName));

        // Assert
        exception.ParamName.ShouldBe(ParamName);
        exception.Message.ShouldStartWith("Invalid Test API url scheme: http://api.example.com/. Test API supports only HTTPS.");
    }

    [Theory]
    [InlineData("ftp://example.com/")]
    [InlineData("file:///C:/api/")]
    public void ParseBaseUrl_OtherScheme_ThrowsArgumentExceptionNamingTheScheme(string url)
    {
        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ParseBaseUrl(url, DisplayName, ParamName));

        // Assert
        exception.ParamName.ShouldBe(ParamName);
        exception.Message.ShouldStartWith($"Invalid Test API url scheme: {url}");
        exception.Message.ShouldNotContain("HTTPS");
    }

    [Theory]
    [InlineData("https://gateway.example.com/api?key=1")]
    [InlineData("https://gateway.example.com/api/?key=1")]
    [InlineData("https://gateway.example.com/api/#fragment")]
    [InlineData("https://gateway.example.com/api#fragment")]
    public void ParseBaseUrl_QueryOrFragment_ThrowsArgumentExceptionPointingToADelegatingHandler(string url)
    {
        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ParseBaseUrl(url, DisplayName, ParamName));

        // Assert
        exception.ParamName.ShouldBe(ParamName);
        exception.Message.ShouldStartWith($"Invalid Test API url: {url}. It must not contain a query or fragment");
    }
}
