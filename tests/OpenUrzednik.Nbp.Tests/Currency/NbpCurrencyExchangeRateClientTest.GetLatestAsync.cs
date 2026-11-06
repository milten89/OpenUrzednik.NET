using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

using TableType = OpenUrzednik.Nbp.Table.TableType;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class NbpCurrencyExchangeRateClientTest
{
    [Theory]
    [InlineData("")]
    [InlineData("US")]
    public async Task GetLatestAsync_InvalidCurrency_ReturnsFailureWithoutSendingRequest(string? currency)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act
        var result = await sut.GetLatestAsync(currency!, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void GetLatestAsync_NullCurrency_ThrowsArgumentNullExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = sut.GetLatestAsync(null!, cancellationToken: TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("currency");
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public void GetLatestAsync_UndefinedTable_ThrowsArgumentOutOfRangeExceptionWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, Substitute.For<INbpUrlBuilderFactory>());

        // Act && Assert
        Should.Throw<ArgumentOutOfRangeException>(() => { _ = sut.GetLatestAsync(currency, (TableType)42, TestContext.Current.CancellationToken); })
            .ParamName.ShouldBe("table");
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData(TableType.A, (int)NbpTable.A)]
    [InlineData(TableType.B, (int)NbpTable.B)]
    public async Task GetLatestAsync_SuccessfulResponse_ReturnsMappedCurrencyExchangeRates(TableType table, int nbpTableValue)
    {
        // Arrange
        var nbpTable = (NbpTable)nbpTableValue;
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        var dto = new CurrencyExchangeRatesDtoFaker().LinkRandomizerTo(faker).Generate();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dto), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(nbpTable, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetLatestAsync(currency, table, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToCurrencyExchangeRates(dto));
        urlBuilderFactory.Received(1).GetCurrencyBuilder(nbpTable, currency);
        urlBuilder.Received(1).Latest();
    }

    [Fact]
    public async Task GetLatestAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var currency = faker.Finance.Currency().Code;
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var urlBuilderFactory = CreateUrlBuilderFactory(NbpTable.A, currency, urlBuilder);
        var sut = CreateApiClient(httpClient, urlBuilderFactory);

        // Act
        var result = await sut.GetLatestAsync(currency, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}
