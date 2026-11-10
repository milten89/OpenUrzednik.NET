using System.Net;

using OpenUrzednik.MF.WhiteList.Common;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListErrorMappingTest
{
    [Theory]
    [InlineData("WL-115")]
    [InlineData("WL-000")]
    [InlineData("WL-999")]
    [InlineData("X")]
    [InlineData("A")]
    [InlineData("Z")]
    public void SanitizeCode_DocumentedShape_KeepsIt(string code)
    {
        // Act
        var sanitized = WhiteListErrorMapping.SanitizeCode(code);

        // Assert
        sanitized.ShouldBe(code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(" ")]
    [InlineData("1234567890")] // an identifier in the code field
    [InlineData("WL-1234567890")]
    [InlineData("WL-11")]
    [InlineData("WL-1150")]
    [InlineData("wl-115")]
    [InlineData("WX-115")]
    [InlineData("WL_115")]
    [InlineData("WL-A15")]
    [InlineData("WL-1A5")]
    [InlineData("WL-11A")]
    [InlineData("WL-/15")] // '/' and ':' are next to '0' and '9' in ASCII
    [InlineData("WL-1:5")]
    [InlineData("x")]
    [InlineData("@")] // '@' and '[' are next to 'A' and 'Z' in ASCII
    [InlineData("[")]
    [InlineData("1")]
    [InlineData("AB")]
    public void SanitizeCode_AnythingElse_ReturnsNull(string? code)
    {
        // Act
        var sanitized = WhiteListErrorMapping.SanitizeCode(code);

        // Assert
        sanitized.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1234567890")]
    [InlineData("WL-1234567890")]
    public async Task MapAsync_CodeOfAnotherShape_EchoesItNowhere(string code)
    {
        // Act
        var error = await SendAsync(Response(HttpStatusCode.BadRequest, Body(code, "Nieprawidłowy parametr.")));

        // Assert
        var badRequest = error.ShouldBeOfType<WhiteListBadRequestError>();
        badRequest.WhiteListCode.ShouldBeNull();
        badRequest.Metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey].ShouldBeNull();
        badRequest.Message.ShouldBe($"MF White List API rejected the request to {RequestPath}: Nieprawidłowy parametr.");
    }
}
