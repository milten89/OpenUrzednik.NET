using System.Text;

using OpenUrzednik.MF.WhiteList.Common;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed partial class WhiteListErrorMappingTest
{
    [Fact]
    public void MessageText_CutInsideASurrogatePair_DropsTheLoneHalf()
    {
        // Arrange: the 500th character is the first half of an emoji
        var bytes = Encoding.UTF8.GetBytes(new string('a', 499) + "\U0001F600tail");

        // Act
        var text = WhiteListErrorMapping.MessageText(bytes, bytes.Length);

        // Assert: what the executor's ReadMessageAsync gives the error mapping for the same body
        text.ShouldBe(new string('a', 499));
    }

    [Fact]
    public void MessageText_LongBody_IsCutAtTheExecutorsLimit()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(new string('a', 700));

        // Act
        var text = WhiteListErrorMapping.MessageText(bytes, bytes.Length);

        // Assert
        text.ShouldBe(new string('a', 500));
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-32")]
    [InlineData("utf-32BE")]
    public void MessageText_ByteOrderMark_DecodesWithItsEncodingAndDropsIt(string encodingName)
    {
        // Arrange
        var encoding = encodingName == "utf-32BE" ? new UTF32Encoding(bigEndian: true, byteOrderMark: true) : Encoding.GetEncoding(encodingName);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(" Komunikat ")).ToArray();

        // Act
        var text = WhiteListErrorMapping.MessageText(bytes, bytes.Length);

        // Assert: decoded, without the mark, trimmed
        text.ShouldBe("Komunikat");
    }

    [Fact]
    public void MessageText_OnlyPartOfTheBuffer_DecodesThatPart()
    {
        // Arrange: the filter's buffer is larger than the body it holds
        var bytes = new byte[64];
        var length = Encoding.UTF8.GetBytes("Komunikat", 0, 9, bytes, 0);

        // Act
        var text = WhiteListErrorMapping.MessageText(bytes, length);

        // Assert
        text.ShouldBe("Komunikat");
    }
}
