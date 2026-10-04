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
    public void MapToBuySellExchangeRateTable_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var dto = new BuySellExchangeRateTableDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRateTable(dto.TableId, dto.TradingDate, dto.PublicationDate, []));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new BuySellExchangeRateTableDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRateTable(dto.TableId, dto.TradingDate, dto.PublicationDate,
            [new TableBuySellRate(dto.Rates[0].CurrencyName, dto.Rates[0].CurrencyCode, dto.Rates[0].Ask, dto.Rates[0].Bid)]));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new BuySellExchangeRateTableDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRateTable(dto.TableId, dto.TradingDate, dto.PublicationDate,
            dto.Rates.Select(x => new TableBuySellRate(x.CurrencyName, x.CurrencyCode, x.Ask, x.Bid)).ToArray()));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullTableDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellExchangeRateTableDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBuySellExchangeRateTable(dto));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_RatesArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var rateDto = new BuySellExchangeRateDtoFaker().WithConstantSeed().Generate();
        var dtoBase = new BuySellExchangeRateTableDtoFaker(0).WithConstantSeed().Generate();
        var dto = new BuySellExchangeRateTableDto()
        {
            TableId = dtoBase.TableId,
            PublicationDate = dtoBase.PublicationDate,
            TradingDate = dtoBase.TradingDate,
            Rates = [rateDto, null!]
        };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBuySellExchangeRateTable(dto));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var dtos = Array.Empty<BuySellExchangeRateTableDto>();

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dtos);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_ArrayWithMultipleElements_ReturnsAllTablesMappedInOriginalOrder()
    {
        // Arrange
        var dtos = new BuySellExchangeRateTableDtoFaker().WithConstantSeed().Generate(2).ToArray();

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dtos);

        // Assert
        result.ShouldBe(dtos.Select(x => new BuySellExchangeRateTable(x.TableId, x.TradingDate, x.PublicationDate,
            [.. x.Rates.Select(r => new TableBuySellRate(r.CurrencyName, r.CurrencyCode, r.Ask, r.Bid))])));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullArray_ThrowsInvalidPayloadException()
    {
        // Arrange
        BuySellExchangeRateTableDto[] dtos = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBuySellExchangeRateTable(dtos));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_ArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dto = new BuySellExchangeRateTableDtoFaker().WithConstantSeed().Generate();
        var dtos = new[] { dto, null! };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBuySellExchangeRateTable(dtos));
    }
}
