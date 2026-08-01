using System.Globalization;
using Bogus;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.TestCommon.Extensions;
using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public class MapperTest
{
    [Fact]
    public void MapToGoldPrice_SingleDto_ReturnsGoldPriceWithMatchingDateAndPrice()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateGoldPriceDto(faker, new DateOnly(2026, 1, 15), 250.75m);

        // Act
        var result = Mapper.MapToGoldPrice(dto);

        // Assert
        result.Date.ShouldBe(dto.Date);
        result.Price.ShouldBe(dto.Price);
    }

    [Fact]
    public void MapToGoldPrice_RandomDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateGoldPriceDto(faker);

        // Act
        var result = Mapper.MapToGoldPrice(dto);

        // Assert
        result.ShouldBe(new GoldPrice(dto.Date, dto.Price));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.0001")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("-100")]
    public void MapToGoldPrice_VariousPriceValues_PreservesExactDecimalValue(string priceAsString)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var price = decimal.Parse(priceAsString, CultureInfo.InvariantCulture);
        var dto = CreateGoldPriceDto(faker, price: price);

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
        var faker = new Faker().WithConstantSeed();
        var date = new DateOnly(year, month, day);
        var dto = CreateGoldPriceDto(faker, date: date);

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
        var faker = new Faker().WithConstantSeed();
        var dto = CreateGoldPriceDto(faker);
        var dtos = new[] { dto };

        // Act
        var result = Mapper.MapToGoldPrice(dtos);

        // Assert
        result.Length.ShouldBe(1);
        result[0].ShouldBe(new GoldPrice(dto.Date, dto.Price));
    }

    [Fact]
    public void MapToGoldPrice_ArrayWithMultipleElements_ReturnsAllElementsMappedInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dtos = new[]
        {
            CreateGoldPriceDto(faker, new DateOnly(2026, 1, 1), 100m),
            CreateGoldPriceDto(faker, new DateOnly(2026, 1, 2), 200m),
            CreateGoldPriceDto(faker, new DateOnly(2026, 1, 3), 300m),
        };

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
        var faker = new Faker().WithConstantSeed();
        var dto = CreateGoldPriceDto(faker);
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

        // Act
        var exception = Record.Exception(() => Mapper.MapToGoldPrice(dtos));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");;
    }
    
    [Fact]
    public void MapToGoldPrice_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        GoldPriceDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToGoldPrice(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");;
    }
    
    [Fact]
    public void MapToGoldPrice_ArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dtos = new GoldPriceDto[] { CreateGoldPriceDto(faker), null! };

        // Act
        var exception = Record.Exception(() => Mapper.MapToGoldPrice(dtos));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");;
    }
    
    private static GoldPriceDto CreateGoldPriceDto(Faker faker, DateOnly? date = null, decimal? price = null)
        => new()
        {
            Date = date ?? DateOnly.FromDateTime(faker.Date.Past()),
            Price = price ?? faker.Finance.Amount(1, 500)
        };
}