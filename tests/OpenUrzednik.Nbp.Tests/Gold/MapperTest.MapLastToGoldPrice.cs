using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class MapperTest
{
    [Fact]
    public void MapLastToGoldPrice_UnsortedArray_ReturnsPriceWithLatestDate()
    {
        // Arrange
        var dtos = new GoldPriceDtoFaker().WithConstantSeed().Generate(5).ToArray();
        var expected = dtos.OrderByDescending(x => x.Date).First();

        // Act
        var result = Mapper.MapLastToGoldPrice(dtos);

        // Assert
        result.ShouldBe(new GoldPrice(expected.Date.ToPublicDate(), expected.Price));
    }

    [Fact]
    public void MapLastToGoldPrice_EmptyArray_ReturnsNull()
    {
        // Act
        var result = Mapper.MapLastToGoldPrice([]);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void MapLastToGoldPrice_NullArray_ThrowsInvalidPayloadException()
    {
        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToGoldPrice(null!));
    }

    [Fact]
    public void MapLastToGoldPrice_ArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        GoldPriceDto[] dtos = [new GoldPriceDtoFaker().WithConstantSeed().Generate(), null!];

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToGoldPrice(dtos));
    }
}
