using Bogus;

using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public class MapperTest
{
    private static readonly DateTime FixedDateRangeStart = new(2000, 1, 1);
    private static readonly DateTime FixedDateRangeEnd = new(2030, 12, 31);
    
    [Fact]
    public void MapToCurrencyExchangeRateDto_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateCurrencyExchangeRateDto(faker,"001/A/NBP/2026", new DateOnly(2026, 1, 2), 4.05m);

        // Act
        var result = Mapper.MapToCurrencyExchangeRateDto(dto);

        // Assert
        result.ShouldBe(new ExchangeRate(dto.TableId, dto.PublicationDate, dto.Price));
    }

    [Fact]
    public void MapToCurrencyExchangeRateDto_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CurrencyExchangeRateDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToCurrencyExchangeRateDto(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    // ---- MapToCurrencyExchangeRates(CurrencyExchangeRatesDto) -> CurrencyExchangeRates ----

    [Fact]
    public void MapToCurrencyExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateCurrencyExchangeRatesDto(faker,[]);

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDto = CreateCurrencyExchangeRateDto(faker);
        var dto = CreateCurrencyExchangeRatesDto(faker, [rateDto]);

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            [new ExchangeRate(rateDto.TableId, rateDto.PublicationDate, rateDto.Price)]));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDtos = new[]
        {
            CreateCurrencyExchangeRateDto(faker, "001/A/NBP/2026", new DateOnly(2026, 1, 2), 4.00m),
            CreateCurrencyExchangeRateDto(faker, "002/A/NBP/2026", new DateOnly(2026, 1, 3), 4.05m),
            CreateCurrencyExchangeRateDto(faker, "003/A/NBP/2026", new DateOnly(2026, 1, 4), 4.10m),
        };
        var dto = CreateCurrencyExchangeRatesDto(faker, rateDtos);

        // Act
        var result = Mapper.MapToCurrencyExchangeRates(dto);

        // Assert
        result.ShouldBe(new CurrencyExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            rateDtos.Select(x => new ExchangeRate(x.TableId, x.PublicationDate, x.Price)).ToArray()));
    }

    [Fact]
    public void MapToCurrencyExchangeRates_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CurrencyExchangeRatesDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToCurrencyExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToCurrencyExchangeRates_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateCurrencyExchangeRatesDto(faker, [CreateCurrencyExchangeRateDto(faker), null!]);

        // Act
        var exception = Record.Exception(() => Mapper.MapToCurrencyExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToCountryExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateCountryExchangeRatesDto(faker, []);

        // Act
        var result = Mapper.MapToCountryExchangeRates(dto);

        // Assert
        result.ShouldBe(new CountryExchangeRates(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToCountryExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDto = CreateCurrencyExchangeRateDto(faker);
        var dto = CreateCountryExchangeRatesDto(faker, [rateDto]);

        // Act
        var result = Mapper.MapToCountryExchangeRates(dto);

        // Assert
        result.ShouldBe(new CountryExchangeRates(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode,
            [new ExchangeRate(rateDto.TableId, rateDto.PublicationDate, rateDto.Price)]));
    }

    [Fact]
    public void MapToCountryExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDtos = new[]
        {
            CreateCurrencyExchangeRateDto(faker, "001/B/NBP/2026", new DateOnly(2026, 1, 2), 5.00m),
            CreateCurrencyExchangeRateDto(faker, "002/B/NBP/2026", new DateOnly(2026, 1, 3), 5.05m),
        };
        var dto = CreateCountryExchangeRatesDto(faker, rateDtos);

        // Act
        var result = Mapper.MapToCountryExchangeRates(dto);

        // Assert
        result.ShouldBe(new CountryExchangeRates(dto.Country, dto.CurrencyName, dto.CurrencySymbol, dto.CurrencyCode,
            rateDtos.Select(x => new ExchangeRate(x.TableId, x.PublicationDate, x.Price)).ToArray()));
    }

    [Fact]
    public void MapToCountryExchangeRates_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CountryExchangeRatesDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToCountryExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToCountryExchangeRates_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateCountryExchangeRatesDto(faker, [CreateCurrencyExchangeRateDto(faker), null!]);

        // Act
        var exception = Record.Exception(() => Mapper.MapToCountryExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRate_ValidDto_MapsAllFieldsCorrectly()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateBuySellCurrencyExchangeRateDto(faker, "001/C/NBP/2026", new DateOnly(2026, 1, 2), 4.00m, 4.10m);

        // Act
        var result = Mapper.MapToBuySellExchangeRate(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRate(dto.TableId, dto.PublicationDate, dto.Buy, dto.Sell));
    }

    [Fact]
    public void MapToBuySellExchangeRate_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellCurrencyExchangeRateDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRate(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    // ---- MapToBuySellExchangeRates(BuySellCurrencyExchangeRatesDto) -> BuySellExchangeRates ----

    [Fact]
    public void MapToBuySellExchangeRates_EmptyRates_ReturnsEmptyRatesList()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateBuySellCurrencyExchangeRatesDto(faker, []);

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode, []));
    }

    [Fact]
    public void MapToBuySellExchangeRates_SingleRate_ReturnsSingleMappedRate()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDto = CreateBuySellCurrencyExchangeRateDto(faker);
        var dto = CreateBuySellCurrencyExchangeRatesDto(faker, [rateDto]);

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            [new BuySellExchangeRate(rateDto.TableId, rateDto.PublicationDate, rateDto.Buy, rateDto.Sell)]));
    }

    [Fact]
    public void MapToBuySellExchangeRates_MultipleRates_ReturnsAllRatesMappedInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var rateDtos = new[]
        {
            CreateBuySellCurrencyExchangeRateDto(faker, "001/C/NBP/2026", new DateOnly(2026, 1, 2), 4.00m, 4.05m),
            CreateBuySellCurrencyExchangeRateDto(faker, "002/C/NBP/2026", new DateOnly(2026, 1, 3), 4.06m, 4.11m),
        };
        var dto = CreateBuySellCurrencyExchangeRatesDto(faker, rateDtos);

        // Act
        var result = Mapper.MapToBuySellExchangeRates(dto);

        // Assert
        result.ShouldBe(new BuySellExchangeRates(dto.CurrencyName, dto.CurrencyCode,
            rateDtos.Select(x => new BuySellExchangeRate(x.TableId, x.PublicationDate, x.Buy, x.Sell)).ToArray()));
    }

    [Fact]
    public void MapToBuySellExchangeRates_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        BuySellCurrencyExchangeRatesDto dto = null!;

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }

    [Fact]
    public void MapToBuySellExchangeRates_RatesArrayContainingNullElement_ThrowsArgumentNullException()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = CreateBuySellCurrencyExchangeRatesDto(faker, [CreateBuySellCurrencyExchangeRateDto(faker), null!]);

        // Act
        var exception = Record.Exception(() => Mapper.MapToBuySellExchangeRates(dto));

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("dto");
    }
    
    private static DateOnly RandomDate(Faker faker)
        => DateOnly.FromDateTime(faker.Date.Between(FixedDateRangeStart, FixedDateRangeEnd));
    
    private static CurrencyExchangeRateDto CreateCurrencyExchangeRateDto(Faker faker, string? tableId = null, DateOnly? date = null, decimal? price = null)
        => new()
        {
            TableId = tableId ?? faker.Random.AlphaNumeric(10),
            PublicationDate = date ?? RandomDate(faker),
            Price = price ?? faker.Finance.Amount(1, 10)
        };

    private static BuySellCurrencyExchangeRateDto CreateBuySellCurrencyExchangeRateDto(Faker faker, string? tableId = null, DateOnly? date = null, decimal? buy = null, decimal? sell = null)
        => new()
        {
            TableId = tableId ?? faker.Random.AlphaNumeric(10),
            PublicationDate = date ?? RandomDate(faker),
            Buy = buy ?? faker.Finance.Amount(1, 10),
            Sell = sell ?? faker.Finance.Amount(1, 10)
        };

    private static CurrencyExchangeRatesDto CreateCurrencyExchangeRatesDto(Faker faker, CurrencyExchangeRateDto[]? rates = null)
        => new()
        {
            CurrencyName = faker.Commerce.ProductName(),
            CurrencyCode = "USD",
            Rates = rates ?? []
        };

    private static CountryExchangeRatesDto CreateCountryExchangeRatesDto(Faker faker, CurrencyExchangeRateDto[]? rates = null)
        => new()
        {
            Country = faker.Address.Country(),
            CurrencyName = faker.Commerce.ProductName(),
            CurrencySymbol = "$",
            CurrencyCode = "USD",
            Rates = rates ?? []
        };

    private static BuySellCurrencyExchangeRatesDto CreateBuySellCurrencyExchangeRatesDto(Faker faker, BuySellCurrencyExchangeRateDto[]? rates = null)
        => new()
        {
            CurrencyName = faker.Commerce.ProductName(),
            CurrencyCode = "USD",
            Rates = rates ?? []
        };
}