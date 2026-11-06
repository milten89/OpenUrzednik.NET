using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class DisplayUriRequestOptionTest
{
    [Fact]
    public void TryGet_Set_ReturnsDisplayUri()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/search/nip/9876543210");
        DisplayUriRequestOption.Set(request, "https://api.example.com/api/search/nip/{nip}");

        // Act
        var found = DisplayUriRequestOption.TryGet(request, out var displayUri);

        // Assert
        found.ShouldBeTrue();
        displayUri.ShouldBe("https://api.example.com/api/search/nip/{nip}");
    }

    [Fact]
    public void TryGet_NotSet_ReturnsFalse()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/api/rates");

        // Act
        var found = DisplayUriRequestOption.TryGet(request, out var displayUri);

        // Assert
        found.ShouldBeFalse();
        displayUri.ShouldBeNull();
    }
}
