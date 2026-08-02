using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
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
        using var sut = CreateClient(faker, urlBuilder, new HttpResponseMessage(HttpStatusCode.OK), out var handler);

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
        using var sut = CreateClient(faker, urlBuilder, CreateJsonResponse(HttpStatusCode.OK, [dto]), out _);

        // Act
        var result = await sut.GetAsync(dto.Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(dto.Date, dto.Price));
    }
}