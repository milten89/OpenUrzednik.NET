using OpenUrzednik.Http.DependencyInjection;

using Shouldly;

namespace OpenUrzednik.Http.Tests.DependencyInjection;

public partial class DisplayPathHttpClientLoggerTest
{
    [Theory]
    [InlineData("https://api.example.com/api/rates", "https://api.example.com/api/rates")]
    [InlineData("https://api.example.com/api/rates?date=2026-11-06", "https://api.example.com/api/rates?*")]
    [InlineData("https://user:secret@api.example.com:8443/api/rates?date=2026-11-06#part", "https://api.example.com:8443/api/rates?*")]
    [InlineData("http://api.example.com:80/api/rates", "http://api.example.com/api/rates")]
    public void RedactUri_AbsoluteUri_KeepsPathAndRedactsQuery(string uri, string expected)
    {
        // Act
        var redacted = DisplayPathHttpClientLogger.RedactUri(new Uri(uri));

        // Assert
        redacted.ShouldBe(expected);
    }

    [Fact]
    public void RedactUri_RelativeUri_ReturnsAsterisk()
    {
        // Act
        var redacted = DisplayPathHttpClientLogger.RedactUri(new Uri("api/rates?date=2026-11-06", UriKind.Relative));

        // Assert
        redacted.ShouldBe("*");
    }

    [Fact]
    public void RedactUri_Null_ReturnsNull()
    {
        // Act
        var redacted = DisplayPathHttpClientLogger.RedactUri(null);

        // Assert
        redacted.ShouldBeNull();
    }
}
