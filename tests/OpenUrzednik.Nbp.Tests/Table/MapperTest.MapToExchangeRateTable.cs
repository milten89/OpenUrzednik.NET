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
    public void MapToExchangeRateTable_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var dto = new ExchangeRateTableDtoFaker(0).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new ExchangeRateTable(dto.TableId, dto.PublicationDate, []));
    }

    [Fact]
    public void MapToExchangeRateTable_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var dto = new ExchangeRateTableDtoFaker(1).WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new ExchangeRateTable(dto.TableId, dto.PublicationDate,
            [new ExchangeRate(dto.Rates[0].CurrencyName, dto.Rates[0].CurrencyCode, dto.Rates[0].Price)]));
    }

    [Fact]
    public void MapToExchangeRateTable_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var dto = new ExchangeRateTableDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new ExchangeRateTable(dto.TableId, dto.PublicationDate,
            [.. dto.Rates.Select(x => new ExchangeRate(x.CurrencyName, x.CurrencyCode, x.Price))]));
    }

    [Fact]
    public void MapToExchangeRateTable_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        ExchangeRateTableDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToExchangeRateTable(dto));
    }

    [Fact]
    public void MapToExchangeRateTable_RatesArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dtoBase = new ExchangeRateTableDtoFaker(0).WithConstantSeed().Generate();
        var dto = new ExchangeRateTableDto()
        {
            TableId = dtoBase.TableId,
            PublicationDate = dtoBase.PublicationDate,
            Rates = null!
        };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToExchangeRateTable(dto));
    }

    [Fact]
    public void MapToExchangeRateTable_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var dtos = Array.Empty<ExchangeRateTableDto>();

        // Act
        var result = Mapper.MapToExchangeRateTable(dtos);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void MapToExchangeRateTable_ArrayWithMultipleElements_ReturnsAllTablesMappedInOriginalOrder()
    {
        // Arrange
        var dtos = new ExchangeRateTableDtoFaker().WithConstantSeed().Generate(3).ToArray();

        // Act
        var result = Mapper.MapToExchangeRateTable(dtos);

        // Assert
        result.ShouldBe(dtos.Select(x => new ExchangeRateTable(x.TableId, x.PublicationDate,
            x.Rates.Select(r => new ExchangeRate(r.CurrencyName, r.CurrencyCode, r.Price)).ToArray())));
    }

    [Fact]
    public void MapToExchangeRateTable_NullArray_ThrowsInvalidPayloadException()
    {
        // Arrange
        ExchangeRateTableDto[] dtos = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToExchangeRateTable(dtos));
    }

    [Fact]
    public void MapToExchangeRateTable_ArrayContainingNullElement_ThrowsInvalidPayloadException()
    {
        // Arrange
        var dto = new ExchangeRateTableDtoFaker().WithConstantSeed().Generate();
        var dtos = new[] { dto, null! };

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToExchangeRateTable(dtos));
    }
}
