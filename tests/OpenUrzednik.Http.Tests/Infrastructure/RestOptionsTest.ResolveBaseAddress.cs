using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestOptionsTest
{
    private const string DefaultUrl = "https://default.example.com/api/";

    [Fact]
    public void ResolveBaseAddress_OptionsUrlAndBaseAddress_ParsesOptionsUrlForOptions()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://other.example.com/") };
        var parse = new RecordingParse();

        // Act
        var baseAddress = RestOptions.ResolveBaseAddress("https://proxy.example.com/api", httpClient, DefaultUrl, parse.Parse);

        // Assert
        parse.Calls.ShouldHaveSingleItem().ShouldBe(("https://proxy.example.com/api", "options"));
        baseAddress.ShouldBeSameAs(parse.Result);
    }

    [Fact]
    public void ResolveBaseAddress_HttpClientBaseAddressOnly_ParsesItsOriginalStringForHttpClient()
    {
        // Arrange
        // OriginalString keeps the missing trailing slash, which the parse adds.
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://gateway.example.com/api") };
        var parse = new RecordingParse();

        // Act
        var baseAddress = RestOptions.ResolveBaseAddress(null, httpClient, DefaultUrl, parse.Parse);

        // Assert
        parse.Calls.ShouldHaveSingleItem().ShouldBe(("https://gateway.example.com/api", "httpClient"));
        baseAddress.ShouldBeSameAs(parse.Result);
    }

    [Fact]
    public void ResolveBaseAddress_NoOptionsUrlAndNoBaseAddress_UsesDefaultUnparsed()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var parse = new RecordingParse();

        // Act
        var baseAddress = RestOptions.ResolveBaseAddress(null, httpClient, DefaultUrl, parse.Parse);

        // Assert
        parse.Calls.ShouldBeEmpty();
        baseAddress.ShouldBe(new Uri(DefaultUrl));
    }

    [Fact]
    public void ResolveBaseAddress_Always_LeavesHttpClientUnchanged()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        RestOptions.ResolveBaseAddress("https://proxy.example.com/", httpClient, DefaultUrl, new RecordingParse().Parse);

        // Assert
        httpClient.BaseAddress.ShouldBeNull();
    }

    [Fact]
    public void ResolveBaseAddress_ParseThrows_LetsTheExceptionThrough()
    {
        // Arrange
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://api.example.com/") };

        // Act
        var exception = Should.Throw<ArgumentException>(() => RestOptions.ResolveBaseAddress(null, httpClient, DefaultUrl,
            (url, paramName) => RestOptions.ParseBaseUrl(url, DisplayName, paramName)));

        // Assert
        exception.ParamName.ShouldBe("httpClient");
    }

    private sealed class RecordingParse
    {
        public Uri Result { get; } = new("https://parsed.example.com/");

        public List<(string Url, string ParamName)> Calls { get; } = [];

        public Uri Parse(string url, string paramName)
        {
            Calls.Add((url, paramName));
            return Result;
        }
    }
}
