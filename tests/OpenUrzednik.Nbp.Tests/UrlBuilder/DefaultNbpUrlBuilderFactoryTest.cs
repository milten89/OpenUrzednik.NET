using Bogus;

using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.UrlBuilder;

public class DefaultNbpUrlBuilderFactoryTest
{
    [Theory]
    [InlineData(NbpTable.A, "a")]
    [InlineData(NbpTable.B, "b")]
    [InlineData(NbpTable.C, "c")]
    public void GetTableBuilder_ValidTable_ReturnsBuilderForExpectedPath(NbpTable table, string expectedSegment)
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var builder = sut.GetTableBuilder(table);
 
        // Assert
        builder.Latest().ShouldBe($"exchangerates/tables/{expectedSegment}");
    }
 
    [Theory]
    [InlineData((NbpTable)99)]
    [InlineData((NbpTable)(-1))]
    public void GetTableBuilder_InvalidTable_ThrowsArgumentException(NbpTable table)
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act && Assert
        Should.Throw<ArgumentException>(() => sut.GetTableBuilder(table))
            .ParamName.ShouldBe("table");
    }
 
    [Fact]
    public void GetTableBuilder_CalledTwiceWithSameTable_ReturnsSameCachedInstance()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var first = sut.GetTableBuilder(NbpTable.A);
        var second = sut.GetTableBuilder(NbpTable.A);
 
        // Assert
        second.ShouldBeSameAs(first);
    }
 
    [Fact]
    public void GetTableBuilder_SameTableOnDifferentFactoryInstances_ReturnsSameCachedInstance()
    {
        // Arrange
        var first = new DefaultNbpUrlBuilderFactory();
        var second = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var firstBuilder = first.GetTableBuilder(NbpTable.B);
        var secondBuilder = second.GetTableBuilder(NbpTable.B);
 
        // Assert
        secondBuilder.ShouldBeSameAs(firstBuilder);
    }
 
    [Theory]
    [InlineData(NbpTable.A, "a")]
    [InlineData(NbpTable.B, "b")]
    [InlineData(NbpTable.C, "c")]
    public void GetCurrencyBuilder_ValidTableAndCurrency_ReturnsBuilderForExpectedPath(NbpTable table, string expectedSegment)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var builder = sut.GetCurrencyBuilder(table, currency);
 
        // Assert
        builder.Latest().ShouldBe($"exchangerates/rates/{expectedSegment}/{currency}");
    }
 
    [Fact]
    public void GetCurrencyBuilder_CurrencyRequiringEscaping_ReturnsBuilderWithEscapedSegment()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var builder = sut.GetCurrencyBuilder(NbpTable.A, "EU R/X");
 
        // Assert
        builder.Latest().ShouldBe("exchangerates/rates/a/EU%20R%2FX");
    }
 
    [Theory]
    [InlineData((NbpTable)99)]
    [InlineData((NbpTable)(-1))]
    public void GetCurrencyBuilder_InvalidTable_ThrowsArgumentException(NbpTable table)
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act && Assert
        Should.Throw<ArgumentException>(() => sut.GetCurrencyBuilder(table, "EUR"))
            .ParamName.ShouldBe("table");
    }
 
    [Fact]
    public void GetCurrencyBuilder_CalledTwiceWithSameTableAndCurrency_ReturnsSameCachedInstance()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var first = sut.GetCurrencyBuilder(NbpTable.A, currency);
        var second = sut.GetCurrencyBuilder(NbpTable.A, currency);
 
        // Assert
        second.ShouldBeSameAs(first);
    }
 
    [Fact]
    public void GetCurrencyBuilder_DifferentCurrenciesForSameTable_ReturnsDifferentInstances()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currencyA = faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var currencyB = faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ") + "X";
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var first = sut.GetCurrencyBuilder(NbpTable.A, currencyA);
        var second = sut.GetCurrencyBuilder(NbpTable.A, currencyB);
 
        // Assert
        second.ShouldNotBeSameAs(first);
    }
 
    [Fact]
    public void GetGoldBuilder_ReturnsBuilderForExpectedPath()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var builder = sut.GetGoldBuilder();
 
        // Assert
        builder.Latest().ShouldBe("cenyzlota");
    }
 
    [Fact]
    public void GetGoldBuilder_CalledTwice_ReturnsSameCachedInstance()
    {
        // Arrange
        var sut = new DefaultNbpUrlBuilderFactory();
 
        // Act
        var first = sut.GetGoldBuilder();
        var second = sut.GetGoldBuilder();
 
        // Assert
        second.ShouldBeSameAs(first);
    }
}