using OpenUrzednik.MF.WhiteList.Common;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListErrorMappingTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("Nieprawidłowy NIP.", "Nieprawidłowy NIP.")]
    [InlineData("WL-115", "WL-115")]
    [InlineData("12345678", "12345678")] // 8 digits: shorter than any identifier
    [InlineData("123456789", "*****6789")] // a 9-digit REGON
    [InlineData("000000000", "*****0000")]
    [InlineData("999999999", "*****9999")]
    [InlineData("/123456789:", "/*****6789:")] // '/' and ':' are next to '0' and '9' in ASCII
    [InlineData("1234567890", "******7890")] // a NIP, from the first character to the last
    [InlineData("NIP 1234567890.", "NIP ******7890.")]
    [InlineData("NIP 1234567890 i REGON 123456789.", "NIP ******7890 i REGON *****6789.")]
    [InlineData("1234567890123456789", "***************6789")]
    [InlineData("a12345678b123456789c", "a12345678b*****6789c")]
    [InlineData("123456789 12345678 987654321", "*****6789 12345678 *****4321")]
    [InlineData("١٢٣٤٥٦٧٨٩", "١٢٣٤٥٦٧٨٩")] // not ASCII digits: the API sends identifiers as ASCII
    public void MaskIdentifiers_Text_MasksEveryRunOfNineOrMoreDigits(string text, string expected)
    {
        // Act
        var masked = WhiteListErrorMapping.MaskIdentifiers(text);

        // Assert
        masked.ShouldBe(expected);
    }

    [Fact]
    public void MaskIdentifiers_NoIdentifier_ReturnsTheSameInstance()
    {
        // Arrange
        var text = "Nieprawidłowy parametr 12345678.";

        // Act
        var masked = WhiteListErrorMapping.MaskIdentifiers(text);

        // Assert
        masked.ShouldBeSameAs(text);
    }
}
