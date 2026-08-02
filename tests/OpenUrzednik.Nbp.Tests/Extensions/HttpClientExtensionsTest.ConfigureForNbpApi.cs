using Bogus;

using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public partial class HttpClientExtensionsTest
{
    [Fact]
    public void ConfigureForNbpApi_DefaultOptions_RetunsConfiguredHttpClient()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var options = new NbpOptions();
        
        // Act
        httpClient.ConfigureForNbpApi(options);
        
        // Assert
        httpClient.BaseAddress.ShouldNotBeNull();
        httpClient.BaseAddress.ToString().ShouldBe(NbpOptions.DefaultApiUrl);
        httpClient.Timeout.ShouldBe(NbpOptions.DefaultTimeout);
    }
    
    [Fact]
    public void ConfigureForNbpApi_ValidOptions_RetunsConfiguredHttpClient()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = new HttpClient();
        var options = new NbpOptions
        {
            ApiUrl = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/',
            Timeout = TimeSpan.FromMilliseconds(faker.Random.Int(1, 30000))
        };
        
        // Act
        httpClient.ConfigureForNbpApi(options);
        
        // Assert
        httpClient.BaseAddress.ShouldNotBeNull();
        httpClient.BaseAddress.ToString().ShouldBe(options.ApiUrl);
        httpClient.Timeout.ShouldBe(options.Timeout);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("api.example.com")]
    [InlineData("www.example.com/api")]
    [InlineData("/api/v1/")]
    [InlineData("ftp://example.com/")]
    [InlineData("https://")]
    [InlineData("https:/example.com")]
    [InlineData("htt ps://example.com")]
    [InlineData("https://example.com:abc/")]
    [InlineData("https://exa mple.com/")]
    public void ConfigureForNbpApi_InvalidApiUrl_ThrowsArgumentException(string? apiUrl)
    {
        // Arrange
        using var httpClient = new HttpClient();
        var options = new NbpOptions()
        {
            ApiUrl = apiUrl!
        };
        
        // Act & Assert
        Should.Throw<ArgumentException>(() => httpClient.ConfigureForNbpApi(options));
    }
    
    [Theory]
    [InlineData("https://api.example.com", "https://api.example.com/")]
    [InlineData("https://api.example.com/", "https://api.example.com/")]
    [InlineData("https://api.example.com/v1", "https://api.example.com/v1/")]
    [InlineData("https://api.example.com/v1/", "https://api.example.com/v1/")]
    public void ConfigureForNbpApi_ValidApiUrl_AddsTrailingSlashOnlyWhenMissing(string input, string expected)
    {
        // Arrange
        using var httpClient = new HttpClient();
        var options = new NbpOptions()
        {
            ApiUrl = input!
        };
        
        // Act
        httpClient.ConfigureForNbpApi(options);
        
        // Assert
        httpClient.BaseAddress.ShouldNotBeNull();
        httpClient.BaseAddress.ToString().ShouldBe(expected);
    }

    [Fact]
    public void ConfigureForNbpApi_InfiniteTimeout_ReturnsConfiguredHttpClient()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var options = new NbpOptions()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        
        // Act
        httpClient.ConfigureForNbpApi(options);
        
        // Assert
        httpClient.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ConfigureForNbpApi_InvalidTimeout_ThrowsArgumentException(double seconds)
    {
        // Arrange
        using var httpClient = new HttpClient();
        var options = new NbpOptions()
        {
            Timeout = TimeSpan.FromSeconds(seconds)
        };
        
        // Act & Assert
        Should.Throw<ArgumentException>(() => httpClient.ConfigureForNbpApi(options));
    }
}