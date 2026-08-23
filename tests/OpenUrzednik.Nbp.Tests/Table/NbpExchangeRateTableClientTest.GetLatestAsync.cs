using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class NbpExchangeRateTableClientTest
{
    [Theory]
    [InlineData((TableType)99)]
    [InlineData((TableType)(-1))]
    public async Task GetLatestAsync_InvalidTable_ReturnsFailureWithoutSendingRequest(TableType table)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);

        // Act
        var result = await sut.GetLatestAsync(table, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData(TableType.A)]
    [InlineData(TableType.B)]
    public async Task GetLatestAsync_SuccessfulResponse_ReturnsMappedExchangeRateTableFromFirstArrayElement(TableType table)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var nbpTable = Mapper.MapToNbpTable(table);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        var dto = new ExchangeRateTableDtoFaker().LinkRandomizerTo(faker).Generate();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, [dto]), out _);
        var sut = CreateApiClient(httpClient, nbpTable, urlBuilder);

        // Act
        var result = await sut.GetLatestAsync(table, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Mapper.MapToExchangeRateTable(dto));
    }

    [Fact]
    public async Task GetLatestAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);

        // Act
        var result = await sut.GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetLatestAsync_EmptyArrayResponse_ReturnFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Latest().Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, Array.Empty<ExchangeRateTableDto>()), out _);
        var sut = CreateApiClient(httpClient, NbpTable.A, urlBuilder);

        // Act
        var result = await sut.GetLatestAsync(TableType.A, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}
