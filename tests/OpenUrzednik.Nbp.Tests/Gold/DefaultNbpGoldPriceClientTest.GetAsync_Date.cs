using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Theory]
    [InlineData(2013, 1, 1)]
    [InlineData(2000, 1, 1)]
    public async Task GetAsync_DateBeforeMinDate_ReturnsFailureWithoutSendingRequest(int year, int month, int day)
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(new DateOnly(year, month, day), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_Date_SuccessfulResponse_ReturnsMappedGoldPrice()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var dto = new GoldPriceDtoFaker().LinkRandomizerTo(faker).Generate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(dto.Date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, [dto]), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(dto.Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(dto.Date, dto.Price));
    }

    [Fact]
    public async Task GetAsync_Date_EmptyArrayResponse_ReturnsFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterGoldMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, []), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(date, TestContext.Current.CancellationToken);

        // Arrange
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetAsync_Date_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var date = faker.Date.AfterGoldMinDate();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDate(date).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.NotFound), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}
