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
    public void MapLastToBuySellExchangeRateTable_UnsortedArray_ReturnsTableWithLatestPublicationDate()
    {
        // Arrange
        var dtos = new BuySellExchangeRateTableDtoFaker().WithConstantSeed().Generate(5).ToArray();
        var expected = dtos.OrderByDescending(x => x.PublicationDate).First();

        // Act
        var result = Mapper.MapLastToBuySellExchangeRateTable(dtos);

        // Assert
        result.ShouldBe(Mapper.MapToBuySellExchangeRateTable(expected));
    }

    [Fact]
    public void MapLastToBuySellExchangeRateTable_EmptyArray_ReturnsNull()
    {
        // Act
        var result = Mapper.MapLastToBuySellExchangeRateTable([]);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void MapLastToBuySellExchangeRateTable_ArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellExchangeRateTableDto[] dtos = [new BuySellExchangeRateTableDtoFaker().WithConstantSeed().Generate(), null!];

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapLastToBuySellExchangeRateTable(dtos));
    }
}
