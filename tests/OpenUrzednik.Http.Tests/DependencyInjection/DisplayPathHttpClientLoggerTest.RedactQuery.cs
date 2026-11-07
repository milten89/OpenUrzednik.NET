using OpenUrzednik.Http.DependencyInjection;

using Shouldly;

namespace OpenUrzednik.Http.Tests.DependencyInjection;

public partial class DisplayPathHttpClientLoggerTest
{
    [Theory]
    [InlineData("https://api.example.com/api/search/nip/{nip}", "https://api.example.com/api/search/nip/{nip}")]
    [InlineData("https://api.example.com/api/search/nip/{nip}?date=2026-11-06", "https://api.example.com/api/search/nip/{nip}?*")]
    [InlineData("https://api.example.com/api/search/nip/{nip}?", "https://api.example.com/api/search/nip/{nip}?")]
    [InlineData("?nip={nip}&date=2026-11-06", "?*")]
    public void RedactQuery_DisplayUri_ReplacesNonEmptyQuery(string displayUri, string expected)
    {
        // Act
        var redacted = DisplayPathHttpClientLogger.RedactQuery(displayUri);

        // Assert
        redacted.ShouldBe(expected);
    }
}
