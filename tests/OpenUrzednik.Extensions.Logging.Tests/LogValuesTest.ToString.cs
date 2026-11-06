using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class LogValuesTest
{
    [Fact]
    public void ToString_PlaceholdersMatchValues_FillsThemInOrder()
    {
        // Arrange
        var sut = Create("{provider} request to {path} failed", ("provider", "NBP API"), ("path", "cenyzlota"));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("NBP API request to cenyzlota failed");
    }

    [Fact]
    public void ToString_PlaceholderNamesDiffer_FillsByPosition()
    {
        // Arrange
        var sut = Create("{first} {second}", ("b", 2), ("a", 1));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("2 1");
    }

    [Fact]
    public void ToString_MorePlaceholdersThanValues_KeepsTheRestAsWritten()
    {
        // Arrange
        var sut = Create("{a} and {b}", ("a", 1));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("1 and {b}");
    }

    [Fact]
    public void ToString_EscapedBraces_WritesLiteralBraces()
    {
        // Arrange
        var sut = Create("{{literal}} {a}", ("a", 1));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("{literal} 1");
    }

    [Fact]
    public void ToString_UnclosedBrace_WritesTheRestAsWritten()
    {
        // Arrange
        var sut = Create("value {a", ("a", 1));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("value {a");
    }

    [Fact]
    public void ToString_NullValue_WritesNullMarker()
    {
        // Arrange
        var sut = Create("value {a}", ("a", null));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("value (null)");
    }

    [Fact]
    public void ToString_FormatAndAlignment_AppliesThemWithInvariantCulture()
    {
        // Arrange
        using var _ = new CultureScope("pl-PL");
        var sut = Create("[{price,8:0.00}] {date:yyyy-MM-dd} {amount}", ("price", 3.5m), ("date", new DateOnly(2026, 10, 2)), ("amount", 1.25m));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("[    3.50] 2026-10-02 1.25");
    }

    [Fact]
    public void ToString_InvalidFormat_WritesPlaceholderInsteadOfThrowing()
    {
        // Arrange
        var sut = Create("{a,x}", ("a", 1));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("{a,x}");
    }

    [Fact]
    public void ToString_Collection_JoinsItemsWithInvariantCulture()
    {
        // Arrange
        using var _ = new CultureScope("pl-PL");
        var sut = Create("codes {codes} rates {rates}", ("codes", new[] { "USD", null, "EUR" }), ("rates", new[] { 1.5m, 2.25m }));

        // Act
        var message = sut.ToString();

        // Assert
        message.ShouldBe("codes USD, (null), EUR rates 1.5, 2.25");
    }
}
