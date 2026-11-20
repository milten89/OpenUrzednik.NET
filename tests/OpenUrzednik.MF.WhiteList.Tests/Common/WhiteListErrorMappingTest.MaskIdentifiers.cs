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
    [InlineData("123456789 12345678 987654321", "********* ******** *****4321")] // single spaces join them into one run
    [InlineData("123456789, 12345678, 987654321", "*****6789, 12345678, *****4321")]
    [InlineData("١٢٣٤٥٦٧٨٩", "١٢٣٤٥٦٧٨٩")] // not ASCII digits: the API sends identifiers as ASCII
    // Formatted identifiers: single separators of one style join the digits, which are masked in place.
    [InlineData("NIP 736-812-54-16.", "NIP ***-***-54-16.")]
    [InlineData("NIP 736 812 54 16.", "NIP *** *** 54 16.")]
    [InlineData("NIP 736 812 54 16.", "NIP *** *** 54 16.")] // NBSP
    [InlineData("NIP 736 812 54 16.", "NIP *** *** 54 16.")] // NBSP and space are one style
    [InlineData("REGON 123.456.789", "REGON ***.**6.789")]
    [InlineData("123/456/789", "***/**6/789")]
    [InlineData("Konto 61 1090 1014 0000 0712 1981 2874.", "Konto ** **** **** **** **** **** 2874.")]
    [InlineData("12-345-678", "12-345-678")] // 8 digits
    [InlineData("1-2-3-4-5-6-7-8-9", "*-*-*-*-*-6-7-8-9")]
    [InlineData("123456789-", "*****6789-")] // a separator at the end joins nothing
    [InlineData("-123 456 789", "-*** **6 789")] // nor at the start
    [InlineData("123--456--789", "123--456--789")] // a doubled separator ends the run
    [InlineData("123 -456 -789", "123 -456 -789")]
    [InlineData("123,456,789", "123,456,789")] // ',' isn't a separator
    [InlineData("736-812 54-16", "736-812 54-16")] // mixed styles end the run: none of its parts has nine digits
    [InlineData("1-2 123 456 789", "1-* *** **6 789")] // the next run starts at the last group ("2") with its own style
    [InlineData("123 456 789-12", "*** **6 789-12")] // the style change ends the masked run
    // A number before an identifier, with another separator: the next run starts at the last group, so it still has all the digits.
    [InlineData("12 736-812-54-16", "12 ***-***-54-16")]
    [InlineData("Błąd 3 736-812-54-16.", "Błąd 3 ***-***-54-16.")]
    [InlineData("2026-11-19 10:15:30 736-812-54-16", "2026-11-19 10:15:30 ***-***-54-16")]
    [InlineData("123 456 789 736-812-54-16", "*** *** **9 ***-***-54-16")] // overlapping runs: both are masked
    [InlineData("1-2 3.4 5 736-812-54-16", "1-2 3.4 5 ***-***-54-16")] // several style changes in a row
    [InlineData("12.34 5678-91234", "12.34 ****-*1234")] // the last group starts a run of nine digits
    [InlineData("123.45 678-91", "123.45 678-91")] // ten digits only across three styles: no single run has nine
    // Dates and times: a change of style ends the run before it reaches nine digits.
    [InlineData("2026-11-19 10:15:30", "2026-11-19 10:15:30")]
    [InlineData("19.11.2026 10:15", "19.11.2026 10:15")]
    [InlineData("Data 2026-11-19 12 zapytań", "Data 2026-11-19 12 zapytań")]
    [InlineData("2026-11-19T10:15:30Z", "2026-11-19T10:15:30Z")]
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
