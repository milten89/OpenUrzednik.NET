using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.UrlBuilder;

public class DefaultNbpUrlBuilderTest
{
    [Theory]
    [InlineData("exchangerates/tables/a", "exchangerates/tables/a")]
    [InlineData("exchangerates/tables/a/", "exchangerates/tables/a")]
    [InlineData("cenyzlota", "cenyzlota")]
    [InlineData("cenyzlota/", "cenyzlota")]
    [InlineData("", "")]
    [InlineData("/", "")]
    public void Ctor_BaseUrl_TrimsSingleTrailingSlashOnly(string baseUrl, string expected)
    {
        // Arrange && Act
        var sut = new DefaultNbpUrlBuilder(baseUrl);
 
        // Assert
        sut.Latest().ShouldBe(expected);
    }
 
    [Fact]
    public void Latest_ReturnsBaseUrlUnchanged()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("exchangerates/rates/a/eur");
 
        // Act
        var result = sut.Latest();
 
        // Assert
        result.ShouldBe("exchangerates/rates/a/eur");
    }
 
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(255)]
    public void ForTopCount_ReturnsBaseUrlWithLastSegmentAndCount(int topCount)
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("exchangerates/rates/a/eur");
 
        // Act
        var result = sut.ForTopCount(topCount);
 
        // Assert
        result.ShouldBe($"exchangerates/rates/a/eur/last/{topCount}");
    }
 
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ForTopCount_DoesNotValidateAndFormatsValueVerbatim(int topCount)
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("cenyzlota");
 
        // Act
        var result = sut.ForTopCount(topCount);
 
        // Assert
        result.ShouldBe($"cenyzlota/last/{topCount}");
    }
 
    [Fact]
    public void Today_ReturnsBaseUrlWithTodaySegment()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("exchangerates/tables/a");
 
        // Act
        var result = sut.Today();
 
        // Assert
        result.ShouldBe("exchangerates/tables/a/today");
    }
 
    [Theory]
    [InlineData(2026, 1, 5, "2026-01-05")]
    [InlineData(2026, 12, 31, "2026-12-31")]
    [InlineData(2000, 2, 29, "2000-02-29")]
    public void ForDate_ReturnsBaseUrlWithZeroPaddedIsoDate(int year, int month, int day, string expectedDateSegment)
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("cenyzlota");
        var date = new DateOnly(year, month, day);
 
        // Act
        var result = sut.ForDate(date);
 
        // Assert
        result.ShouldBe($"cenyzlota/{expectedDateSegment}");
    }
 
    [Fact]
    public void ForDateRange_ReturnsBaseUrlWithBothZeroPaddedIsoDates()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("exchangerates/rates/a/eur");
        var from = new DateOnly(2026, 1, 5);
        var to = new DateOnly(2026, 2, 9);
 
        // Act
        var result = sut.ForDateRange(from, to);
 
        // Assert
        result.ShouldBe("exchangerates/rates/a/eur/2026-01-05/2026-02-09");
    }
 
    [Fact]
    public void ForDateRange_FromAfterTo_StillFormatsBothVerbatim()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilder("cenyzlota");
        var from = new DateOnly(2026, 5, 1);
        var to = new DateOnly(2026, 1, 1);
 
        // Act
        var result = sut.ForDateRange(from, to);
 
        // Assert
        result.ShouldBe("cenyzlota/2026-05-01/2026-01-01");
    }
}