using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class MapperTest
{
    [Fact]
    public void MapLastToExchangeRateTable_UnsortedArray_ReturnsTableWithLatestPublicationDate()
    {
        // Arrange
        var dtos = new ExchangeRateTableDtoFaker().WithConstantSeed().Generate(5).ToArray();
        var expected = dtos.OrderByDescending(x => x.PublicationDate).First();

        // Act
        var result = Mapper.MapLastToExchangeRateTable(dtos);

        // Assert
        result.ShouldBe(Mapper.MapToExchangeRateTable(expected));
    }

    [Fact]
    public void MapLastToExchangeRateTable_EmptyArray_ReturnsNull()
    {
        // Act
        var result = Mapper.MapLastToExchangeRateTable([]);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void MapLastToExchangeRateTable_NullArray_ThrowsInvalidPayloadException()
    {
        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToExchangeRateTable(null!))
            .Message.ShouldBe("NBP API response is missing 'item'.");
    }

    [Fact]
    public void MapLastToExchangeRateTable_ArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        ExchangeRateTableDto[] dtos = [new ExchangeRateTableDtoFaker().WithConstantSeed().Generate(), null!];

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToExchangeRateTable(dtos))
            .Message.ShouldBe("NBP API response is missing 'item'.");
    }
}
