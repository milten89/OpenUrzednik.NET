using System.Globalization;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;
using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public class MapperTest
{
    [Fact]
    public void MapToGoldPrice_SingleDto_ReturnsGoldPriceWithMatchingDateAndPrice()
    {
        // Arrange
        var dto = new GoldPriceDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToGoldPrice(dto);

        // Assert
        result.Date.ShouldBe(dto.Date);
        result.Price.ShouldBe(dto.Price);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.0001")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("-100")]
    public void MapToGoldPrice_VariousPriceValues_PreservesExactDecimalValue(string priceAsString)
    {
        // Arrange
        var price = decimal.Parse(priceAsString, CultureInfo.InvariantCulture);
        var dtoBase = new GoldPriceDtoFaker().WithConstantSeed().Generate();
        var dto = new GoldPriceDto() { Date = dtoBase.Date, Price = price };

        // Act
        var result = Mapper.MapToGoldPrice(dto);

        // Assert
        result.Price.ShouldBe(price);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(9999, 12, 31)]
    public void MapToGoldPrice_BoundaryDateValues_PreservesExactDate(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var dtoBase = new GoldPriceDtoFaker().WithConstantSeed().Generate();
        var dto = new GoldPriceDto() { Date = date, Price = dtoBase.Price };

        // Act
        var result = Mapper.MapToGoldPrice(dto);

        // Assert
        result.Date.ShouldBe(date);
    }

    [Fact]
    public void MapToGoldPrice_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var dtos = Array.Empty<GoldPriceDto>();

        // Act
        var result = Mapper.MapToGoldPrice(dtos);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void MapToGoldPrice_ArrayWithSingleElement_ReturnsArrayWithOneMappedElement()
    {
        // Arrange
        var dtos = new GoldPriceDtoFaker().WithConstantSeed().Generate(1).ToArray();

        // Act
        var result = Mapper.MapToGoldPrice(dtos);

        // Assert
        result.Length.ShouldBe(1);
        result[0].ShouldBe(new GoldPrice(dtos[0].Date, dtos[0].Price));
    }

    [Fact]
    public void MapToGoldPrice_ArrayWithMultipleElements_ReturnsAllElementsMappedInOriginalOrder()
    {
        // Arrange
        var dtos = new GoldPriceDtoFaker().WithConstantSeed().Generate(3).ToArray();

        // Act
        var result = Mapper.MapToGoldPrice(dtos);

        // Assert
        result.Length.ShouldBe(dtos.Length);
        for (var i = 0; i < dtos.Length; i++)
            result[i].ShouldBe(new GoldPrice(dtos[i].Date, dtos[i].Price));
    }

    [Fact]
    public void MapToGoldPrice_ArrayWithDuplicateEntries_MapsEachEntryIndependently()
    {
        // Arrange
        var dto = new GoldPriceDtoFaker().WithConstantSeed().Generate();
        var dtos = new[] { dto, dto, dto };

        // Act
        var result = Mapper.MapToGoldPrice(dtos);

        // Assert
        result.Length.ShouldBe(3);
        result.ShouldAllBe(x => x == new GoldPrice(dto.Date, dto.Price));
    }

    [Fact]
    public void MapToGoldPrice_NullArray_ThrowsNullReferenceException()
    {
        // Arrange
        GoldPriceDto[] dtos = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToGoldPrice(dtos))
            .ParamName.ShouldBe("dto");;
    }
    
    [Fact]
    public void MapToGoldPrice_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        GoldPriceDto dto = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToGoldPrice(dto))
            .ParamName.ShouldBe("dto");;
    }
    
    [Fact]
    public void MapToGoldPrice_ArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var dto = new GoldPriceDtoFaker().WithConstantSeed().Generate();
        var dtos = new[] { dto, null! };

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToGoldPrice(dtos))
            .ParamName.ShouldBe("dto");;
    }
}