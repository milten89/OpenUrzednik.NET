using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public class MapperTest
{
    private static readonly DateTime FixedDateRangeStart = new(2000, 1, 1);
    private static readonly DateTime FixedDateRangeEnd = new(2030, 12, 31);
    
    [Theory]
    [InlineData(MidTableType.A, NbpTable.A)]
    [InlineData(MidTableType.B, NbpTable.B)]
    public void MapToNbpTable_DefinedValue_ReturnsMappedNbpTable(MidTableType input, NbpTable expected)
    {
        // Act
        var result = Mapper.MapToNbpTable(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData((MidTableType)99)]
    [InlineData((MidTableType)(-1))]
    public void MapToNbpTable_UndefinedValue_ThrowsArgumentException(MidTableType input)
    {
        // Act
        var exception = Record.Exception(() => Mapper.MapToNbpTable(input));

        // Act & Assert
        exception.ShouldBeOfType<ArgumentException>()
            .ParamName.ShouldBe("table");
    }

    [Fact]
    public void MapToExchangeRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateExchangeRateDto(faker, "Dolar amerykański", "USD", 4.05m);

        // Act
        var result = Mapper.MapToExchangeRate(dto);

        // Assert
        result.ShouldBe(new ExchangeRate(dto.CurrencyName, dto.CurrencyCode, dto.Price));
    }

    [Fact]
    public void MapToExchangeRate_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        ExchangeRateDto dto = null!;

        // Act
        var exception =  Record.Exception(() => Mapper.MapToExchangeRate(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToExchangeRateTable_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateExchangeRateTableDto(faker, rates: []);

        // Act
        var result = Mapper.MapToExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new ExchangeRateTable(dto.TableId, dto.PublicationDate, []));
    }

    [Fact]
    public void MapToExchangeRateTable_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDto = CreateExchangeRateDto(faker);
        var dto = CreateExchangeRateTableDto(faker, rates: [rateDto]);

        // Act
        var result = Mapper.MapToExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new ExchangeRateTable(dto.TableId, dto.PublicationDate,
            [new ExchangeRate(rateDto.CurrencyName, rateDto.CurrencyCode, rateDto.Price)]));
    }

    [Fact]
    public void MapToExchangeRateTable_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDtos = new[]
        {
            CreateExchangeRateDto(faker, "Dolar amerykański", "USD", 4.00m),
            CreateExchangeRateDto(faker, "Euro", "EUR", 4.30m),
            CreateExchangeRateDto(faker, "Frank szwajcarski", "CHF", 4.50m),
        };
        var dto = CreateExchangeRateTableDto(faker, rates: rateDtos);

        // Act
        var result = Mapper.MapToExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new ExchangeRateTable(dto.TableId, dto.PublicationDate,
            rateDtos.Select(x => new ExchangeRate(x.CurrencyName, x.CurrencyCode, x.Price)).ToArray()));
    }

    [Fact]
    public void MapToExchangeRateTable_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        ExchangeRateTableDto dto = null!;

        // Act
        var exception =  Record.Exception(() => Mapper.MapToExchangeRateTable(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToExchangeRateTable_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateExchangeRateTableDto(faker, rates: [CreateExchangeRateDto(faker), null!]);

        // Act
        var exception =  Record.Exception(() => Mapper.MapToExchangeRateTable(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
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
        var faker = new Faker().WithConstantSeed();
        var dtos = new[]
        {
            CreateExchangeRateTableDto(faker, "001/A/NBP/2026", new DateOnly(2026, 1, 2)),
            CreateExchangeRateTableDto(faker, "002/A/NBP/2026", new DateOnly(2026, 1, 3)),
        };

        // Act
        var result = Mapper.MapToExchangeRateTable(dtos);

        // Assert
        result.ShouldBe(dtos.Select(x => new ExchangeRateTable(x.TableId, x.PublicationDate,
            x.Rates.Select(r => new ExchangeRate(r.CurrencyName, r.CurrencyCode, r.Price)).ToArray())));
    }

    [Fact]
    public void MapToExchangeRateTable_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        ExchangeRateTableDto[] dtos = null!;

        // Act
        var exception =  Record.Exception(() => Mapper.MapToExchangeRateTable(dtos));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToExchangeRateTable_ArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dtos = new[] { CreateExchangeRateTableDto(faker), null! };

        // Act
        var exception =  Record.Exception(() => Mapper.MapToExchangeRateTable(dtos));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_ValidRateDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateBuySellExchangeRateDto(faker, "Dolar amerykański", "USD", 4.00m, 4.10m);

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

        // Act
        var exception =  Record.Exception(() => Mapper.MapToBuySellExchangeRateTable(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateBuySellExchangeRateTableDto(faker, rates: []);

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRateTable(dto.TableId, dto.TraidingDate, dto.PublicationDate, []));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDto = CreateBuySellExchangeRateDto(faker);
        var dto = CreateBuySellExchangeRateTableDto(faker, rates: [rateDto]);

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRateTable(dto.TableId, dto.TraidingDate, dto.PublicationDate,
            [new BuySellExchangeRate(rateDto.CurrencyName, rateDto.CurrencyCode, rateDto.Buy, rateDto.Sell)]));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDtos = new[]
        {
            CreateBuySellExchangeRateDto(faker, "Dolar amerykański", "USD", 4.00m, 4.05m),
            CreateBuySellExchangeRateDto(faker, "Euro", "EUR", 4.25m, 4.35m),
        };
        var dto = CreateBuySellExchangeRateTableDto(faker, rates: rateDtos);

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRateTable(dto.TableId, dto.TraidingDate, dto.PublicationDate,
            rateDtos.Select(x => new BuySellExchangeRate(x.CurrencyName, x.CurrencyCode, x.Buy, x.Sell)).ToArray()));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullTableDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellExchangeRateTableDto dto = null!;

        // Act
        var exception =  Record.Exception(() => Mapper.MapToBuySellExchangeRateTable(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateBuySellExchangeRateTableDto(faker, rates: [CreateBuySellExchangeRateDto(faker), null!]);

        // Act
        var exception =  Record.Exception(() => Mapper.MapToBuySellExchangeRateTable(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
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
        var faker = new Faker().WithConstantSeed();
        var dtos = new[]
        {
            CreateBuySellExchangeRateTableDto(faker, "001/C/NBP/2026", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3)),
            CreateBuySellExchangeRateTableDto(faker, "002/C/NBP/2026", new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4)),
        };

        // Act
        var result = Mapper.MapToBuySellExchangeRateTable(dtos);

        // Assert
        result.ShouldBe(dtos.Select(x => new BuySellExchangeRateTable(x.TableId, x.TraidingDate, x.PublicationDate,
            x.Rates.Select(r => new BuySellExchangeRate(r.CurrencyName, r.CurrencyCode, r.Buy, r.Sell)).ToArray())));
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellExchangeRateTableDto[] dtos = null!;

        // Act
        var exception =  Record.Exception(() => Mapper.MapToBuySellExchangeRateTable(dtos));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRateTable_ArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dtos = new[] { CreateBuySellExchangeRateTableDto(faker), null! };

        // Act
        var exception =  Record.Exception(() => Mapper.MapToBuySellExchangeRateTable(dtos));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }
    
    private static DateOnly RandomDate(Faker faker)
        => DateOnly.FromDateTime(faker.Date.Between(FixedDateRangeStart, FixedDateRangeEnd));
    
    private static ExchangeRateDto CreateExchangeRateDto(Faker faker, string? currencyName = null, string? currencyCode = null, decimal? price = null)
        => new()
        {
            CurrencyName = currencyName ?? faker.Commerce.ProductName(),
            CurrencyCode = currencyCode ?? "USD",
            Price = price ?? faker.Finance.Amount(1, 10)
        };

    private static ExchangeRateTableDto CreateExchangeRateTableDto(Faker faker, string? tableId = null, DateOnly? date = null, ExchangeRateDto[]? rates = null)
        => new()
        {
            TableId = tableId ?? faker.Random.AlphaNumeric(10),
            PublicationDate = date ?? RandomDate(faker),
            Rates = rates ?? []
        };

    private static BuySellExchangeRateDto CreateBuySellExchangeRateDto(Faker faker, string? currencyName = null, string? currencyCode = null, decimal? buy = null, decimal? sell = null)
        => new()
        {
            CurrencyName = currencyName ?? faker.Commerce.ProductName(),
            CurrencyCode = currencyCode ?? "USD",
            Buy = buy ?? faker.Finance.Amount(1, 10),
            Sell = sell ?? faker.Finance.Amount(1, 10)
        };

    private static BuySellExchangeRateTableDto CreateBuySellExchangeRateTableDto(Faker faker, string? tableId = null, DateOnly? tradingDate = null, DateOnly? publicationDate = null, BuySellExchangeRateDto[]? rates = null)
        => new()
        {
            TableId = tableId ?? faker.Random.AlphaNumeric(10),
            TraidingDate = tradingDate ?? RandomDate(faker),
            PublicationDate = publicationDate ?? RandomDate(faker),
            Rates = rates ?? []
        };
}