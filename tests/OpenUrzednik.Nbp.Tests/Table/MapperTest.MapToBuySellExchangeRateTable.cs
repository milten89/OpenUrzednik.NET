using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class MapperTest
{
    [Fact]
    public void MapToBuySellExchangeRateTable_ValidRateDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new BuySellExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Buy, dto.Sell));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullRateDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellExchangeRateDto dto = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToBuySellExchangeRateTable(dto))
            .ParamName.ShouldBe("dto");
    }

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
            [new BuySellExchangeRate(dto.Rates[0].CurrencyName, dto.Rates[0].CurrencyCode, dto.Rates[0].Buy, dto.Rates[0].Sell)]));
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
            dto.Rates.Select(x => new BuySellExchangeRate(x.CurrencyName, x.CurrencyCode, x.Buy, x.Sell)).ToArray()));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullTableDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellExchangeRateTableDto dto = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToBuySellExchangeRateTable(dto))
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_RatesArrayContainingNullElement_ThrowsArgumentNullException()
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
        Should.Throw<ArgumentNullException>(() => Mapper.MapToBuySellExchangeRateTable(dto))
            .ParamName.ShouldBe("dto");
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
            [.. x.Rates.Select(r => new BuySellExchangeRate(r.CurrencyName, r.CurrencyCode, r.Buy, r.Sell))])));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellExchangeRateTableDto[] dtos = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToBuySellExchangeRateTable(dtos))
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_ArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var dto = new BuySellExchangeRateTableDtoFaker().WithConstantSeed().Generate();
        var dtos = new[] { dto, null! };

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => Mapper.MapToBuySellExchangeRateTable(dtos))
            .ParamName.ShouldBe("dto");
    }
}
