using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public class RestProviderProfileTest
{
    [Fact]
    public void Ctor_ValidNames_SetsProperties()
    {
        // Act
        var profile = new RestProviderProfile("nbp", "NBP API");

        // Assert
        profile.Name.ShouldBe("nbp");
        profile.DisplayName.ShouldBe("NBP API");
        profile.MapErrorAsync.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "NBP API")]
    [InlineData(" ", "NBP API")]
    [InlineData("nbp", "")]
    public void Ctor_EmptyName_ThrowsArgumentException(string name, string displayName)
    {
        // Act && Assert
        Should.Throw<ArgumentException>(() => new RestProviderProfile(name, displayName));
    }
}
