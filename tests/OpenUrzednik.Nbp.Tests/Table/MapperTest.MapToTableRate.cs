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
    public void MapToTableRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var dto = new ExchangeRateDtoFaker().WithConstantSeed().Generate();

        // Act
        var result = Mapper.MapToTableRate(dto);

        // Assert
        result.ShouldBe(new TableRate(dto.CurrencyName!, dto.CurrencyCode, dto.Price));
    }

    [Fact]
    public void MapToTableRate_NullDto_ThrowsInvalidPayloadException()
    {
        // Arrange
        ExchangeRateDto dto = null!;

        // Act && Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToTableRate(dto));
    }

    [Fact]
    public void MapToTableRate_MissingCurrencyName_ReturnsEmptyCurrencyName()
    {
        // Arrange
        // Tables A and C before 2004-05-04 have no "currency" field.
        var source = new ExchangeRateDtoFaker().WithConstantSeed().Generate();
        var dto = new ExchangeRateDto { CurrencyCode = source.CurrencyCode, Price = source.Price };

        // Act
        var result = Mapper.MapToTableRate(dto);

        // Assert
        result.ShouldBe(new TableRate(string.Empty, source.CurrencyCode, source.Price));
    }
}
